# IMS on Azure: App Service + Functions + Service Bus

One business event ("an invoice was posted") now fans out to independent workers, without the API knowing who they are.

```
 Angular / Postman
        |
        v
 +-----------------------+   SalesInvoicePosted    +-------------------------------------------+
 | Invoice.CqrsAPI       |  PurchaseOrderApproved  |   Azure Service Bus   TOPIC  ims-events   |
 | (Azure App Service)   | ---ReceiptPosted------> |                                           |
 | Post / Approve        |   publish after commit  |  sub invoice-email  (EventType = SalesInvoicePosted)
 +-----------+-----------+                         |  sub stock-watch    (EventType = SalesInvoicePosted)
             |                                     |  sub audit-log      (no filter: everything)
             v                                     +----+------------------+------------------+---+
        Azure SQL                                       |                  |                  |
                                                        v                  v                  v
                                        Invoice.Functions (Azure Function App)
                                        InvoicePostedEmail   StockWatch            AuditLog
                                         PDF -> Blob, email   low stock? -------+   JSON -> Blob
                                                                                |
                                                          QUEUE stock-alerts <--+
                                                                |
                                                                v
                                                          LowStockAlert  (email purchasing)

   Also: OverdueInvoices (timer, daily)      ResendInvoice (HTTP POST /api/invoices/{id}/resend)
```

| Azure service | Role in IMS | What it teaches |
|---|---|---|
| **App Service** | hosts Invoice.CqrsAPI | app settings instead of appsettings.json, managed identity, zip deploy |
| **Functions** | 6 small workers | triggers: Service Bus (topic + queue), Timer, HTTP; idempotency; retries |
| **Service Bus** | topic `ims-events`, queue `stock-alerts` | pub/sub vs queue, filters, peek-lock, dead-letter, duplicate detection |
| **Storage (Blob)** | invoice PDFs, audit log, idempotency markers | Functions host state, cheap durable storage |
| **Application Insights** | logs and failures of both apps | observability |
| **Managed identity + RBAC** | API and Functions reach Service Bus with NO secret | passwordless Azure |

## Part 1 - run everything locally (no Azure subscription needed)

Tools: Docker Desktop, .NET 10 SDK, Azure Functions Core Tools v4 (`func`).

1. **Emulators**
   ```
   cd azure
   copy .env.example .env        # open it, read the licence note, set ACCEPT_EULA=Y
   docker compose up -d          # Service Bus emulator (5672) + Azurite (10000)
   ```
2. **API with Service Bus switched on** (the `emulator` launch profile loads `appsettings.Emulator.json`)
   ```
   dotnet run --project Invoice.CqrsAPI --launch-profile emulator
   ```
   Without that profile `ServiceBus:Enabled` is false and events are only written to the log (the API still works).
3. **Service account for the Functions**: in `Invoice.CqrsAPI.http` run section 1 (register + login). The demo user it creates is `demo<run>`, so put the same name and password in `Invoice.Functions/local.settings.json` (`ImsApi__UserName`, `ImsApi__Password`). Default there: `demo1` / `Passw0rd!` which matches `@run = 1`.
4. **Functions**
   ```
   cd Invoice.Functions
   func start
   ```
   You should see the six functions listed.
5. **Make an event happen**: in `Invoice.CqrsAPI.http` run sections 2 to 6 (category, item, vendor, customer, PO, receipt, invoice) and finally **Post the invoice**.
6. **Watch the Functions terminal**:
   - `InvoicePostedEmailFunction`: PDF fetched, `EMAIL to ...` line
   - `StockWatchFunction`: stock vs minimum (set the item's `minimumStock` above the remaining stock to see a low-stock alert)
   - `LowStockAlertFunction`: `LOW STOCK ...`
   - `AuditLogFunction`: `Audit: stored SalesInvoicePosted as 2026/...`
   Blobs are in Azurite: container `invoice-pdfs`, `audit-log` (use Azure Storage Explorer, "Local & Attached > Emulator").
7. **Try the failure paths** (the real lesson):
   - Post the same invoice's event twice with `functions.http` (resend): the email function runs again because `IsResend = true`. Delete that flag from the message and replay: it logs "already emailed" (idempotency).
   - Stop the API and post/resend: the email function throws, Service Bus redelivers (up to `MaxDeliveryCount` = 5), then the message lands in the **dead-letter queue** of the subscription.
   - Stop the emulator and post an invoice: the API still answers 200, logs "Could not publish ... the event is lost". That is the dual-write problem. Discuss, then see "Next lessons".

## Part 2 - deploy to Azure

```
az login
cd azure
./deploy.ps1 -SqlAdminPassword '<a strong password>'
```
The script creates the resource group, Service Bus (topic, 3 filtered subscriptions, queue), storage, Application Insights, Azure SQL, one Linux B1 App Service plan shared by the Web App and the Function App, turns on managed identities, grants the roles, publishes both projects, and creates the `functions-bot` user.
Afterwards load your IMS database into Azure SQL (bacpac import or your scripts), open `https://<api>.azurewebsites.net/swagger`, and repeat step 5. Logs: Azure portal > Application Insights > Logs / Live metrics.

When class is over: `az group delete -n <resource-group> --yes --no-wait` (Service Bus Standard and the B1 plan are billed while they exist).

## Concepts cheat sheet

| Concept | In this project |
|---|---|
| Queue vs topic | `stock-alerts` is point-to-point (one consumer). `ims-events` fans out to 3 subscriptions. |
| Subscription filter | `invoice-email` and `stock-watch` only receive `EventType = SalesInvoicePosted`. `audit-log` has no filter. |
| Peek-lock | The trigger locks the message, the function runs, success = complete, exception = abandon and retry. |
| Dead-letter queue | After 5 failed deliveries a message moves to `<subscription>/$deadletterqueue` for inspection. |
| At-least-once delivery | The same message can arrive twice, so the email function checks an `.emailed` marker blob first. |
| Duplicate detection | `MessageId` = `EventId` (and `lowstock-{item}-{invoice}`), so a double send is dropped by the broker. |
| Event vs command | Commands are asked (`PostSalesInvoiceCommand`). Events are announced in the past tense (`SalesInvoicePostedEvent`). |
| Managed identity | `ServiceBus__FullyQualifiedNamespace` in the API and `ServiceBusConnection__fullyQualifiedNamespace` in the Functions: no connection string anywhere. |
| Functions never use the DB | They call the API like any client, so business rules stay in one place. |

## Where the code is

- `Invoice.Events`: event classes, topic / queue / subscription names (shared by API and Functions)
- `Invoice.CqrsAPI/Messaging`: `IEventPublisher`, Service Bus publisher, no-op publisher, DI switch
- `Invoice.CqrsAPI/Features/*/Commands`: `PostSalesInvoice`, `ApprovePurchaseOrder`, `PostReceipt` publish after success
- `Invoice.Functions/Functions`: the six functions
- `azure/`: `docker-compose.yml`, `Config.json` (emulator entities), `deploy.ps1`

## Next lessons

1. **Outbox pattern**: write the event to an `OutboxMessages` table in the same SQL transaction as the invoice, publish from a Function. Fixes the "event can be lost" gap.
2. **Real email**: replace `LogEmailSender` with Azure Communication Services Email.
3. **Durable Functions**: orchestrate PO approved -> notify vendor -> wait for receipt -> reminder (a saga).
4. **Key Vault** for the SQL and bot passwords; **Bicep** or `azd` instead of `deploy.ps1`.
5. **Flex Consumption** hosting for the Function App (pay per use) instead of the shared B1 plan.

## If something does not work

| Symptom | Check |
|---|---|
| `func start` cannot reach storage | Azurite container running? `AzureWebJobsStorage` = `UseDevelopmentStorage=true` |
| Functions start but nothing triggers | Emulator running and `Config.json` mounted? API started with `--launch-profile emulator`? |
| Function logs 401 from the API | `ImsApi__UserName/Password` must be a real IMS user (section 1 of the .http file) |
| Emulator container exits | `ACCEPT_EULA=Y` in `.env`; compare compose file with the current Microsoft docs page |
| Package restore warnings in `Invoice.Functions` | Versions in the csproj are a snapshot: update them in NuGet Package Manager |
| 409 when registering the demo user | Already exists. Change `@run` or just log in |
