I’ve added the Azure part to the solution: an event published from the API, six Azure Functions that react to it, a local setup you can run without an Azure subscription, and a deploy script. I couldn’t compile or run any of it here, so please build it once and send me any errors. The NuGet versions in Invoice.Functions.csproj are a snapshot and may need a patch bump.

When you post an invoice, approve a PO or post a receipt, the API publishes an event to a Service Bus topic ims-events. Three subscriptions pick it up, independently of the API:

Function	Trigger	What it does
InvoicePostedEmailFunction	topic ims-events, subscription invoice-email	Fetches the invoice PDF from the API, saves it to Blob Storage and “emails” the customer. It checks a marker blob first, so a duplicate delivery doesn’t send twice.
StockWatchFunction	topic ims-events, subscription stock-watch	Checks stock against each item’s minimum and sends an alert to the queue stock-alerts.
LowStockAlertFunction	queue stock-alerts	Tells purchasing to reorder.
AuditLogFunction	topic ims-events, subscription audit-log (no filter, receives every event)	Stores every event as a JSON blob.
OverdueInvoicesFunction	timer, daily 08:00 UTC	Lists overdue posted invoices and emails accounts.
ResendInvoiceFunction	HTTP POST /api/invoices/{id}/resend	Announces a posted invoice again.
Where things run: “Azure Apps” I took to mean App Service, which hosts Invoice.CqrsAPI. The Functions call the API like any client and never touch the database, so the business rules stay in one place.
Local run:
Service Bus runs in Microsoft’s emulator and storage in Azurite, both started by docker compose in the azure/ folder. Copy .env.example to .env and set ACCEPT_EULA=Y yourself; I left it at N.
Run the API with the emulator launch profile to switch publishing on. By default publishing is off, so the API works without Azure and only logs the events.
Azure deploy: azure/deploy.ps1 creates the resource group, Service Bus, storage, Application Insights, Azure SQL and one shared App Service plan. It turns on managed identities (no secrets for Service Bus), publishes both projects and creates a functions-bot user. Your IMS database schema still has to be loaded into Azure SQL afterwards. It costs money while it exists; delete the resource group when class is over.
Tests: about 112 tests now, including that each of the three handlers publishes the right event, and that events round-trip between the API and the Functions.

Choices you may want to change:

Lost events: if the broker is down, the invoice still posts and the event is lost, with an error logged. That’s deliberate: it’s the setup for the Outbox pattern as a later lesson.
Email: it is only written to the log. A swap to Azure Communication Services Email is a one-class change.
Hosting: the Function App shares the B1 plan to keep teaching costs low; Flex Consumption is the pay-per-use option.

AZURE-GUIDE.md has the architecture diagram, a step-by-step local run, the failure experiments for students (duplicate delivery, retries, dead-letter queue, broker down), a concepts table and troubleshooting.

Do you want the Outbox pattern next, so events can’t be lost, or Durable Functions for a PO → vendor → receipt workflow?