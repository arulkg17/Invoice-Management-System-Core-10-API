using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Invoice.Events;
using Invoice.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Invoice.Functions.Functions;

/// <summary>
/// TRIGGER: subscription "invoice-email" of topic "ims-events" (filter: EventType = SalesInvoicePosted).
///
/// What it does: fetch the invoice PDF from the API, archive it in Blob Storage, email the customer.
///
/// Things to point out to students:
///  * At-least-once delivery: the same message can arrive twice, so the work is IDEMPOTENT
///    (a marker blob remembers that this invoice was already emailed).
///  * Throwing an exception = the message is retried; after MaxDeliveryCount attempts it moves to the
///    dead-letter queue. A function must therefore throw on real failures and must NOT swallow them.
/// </summary>
public sealed class InvoicePostedEmailFunction
{
    private const string PdfContainer = "invoice-pdfs";

    private readonly ImsApiClient _api;
    private readonly BlobServiceClient _blobs;
    private readonly IEmailSender _email;
    private readonly ILogger<InvoicePostedEmailFunction> _logger;

    public InvoicePostedEmailFunction(
        ImsApiClient api,
        BlobServiceClient blobs,
        IEmailSender email,
        ILogger<InvoicePostedEmailFunction> logger)
    {
        _api = api;
        _blobs = blobs;
        _email = email;
        _logger = logger;
    }

    [Function(nameof(InvoicePostedEmailFunction))]
    public async Task Run(
        [ServiceBusTrigger(ServiceBusNames.Topic, ServiceBusNames.InvoiceEmailSubscription, Connection = ServiceBusClientFactory.ConnectionName)]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        var evt = message.Body.ToObjectFromJson<SalesInvoicePostedEvent>()
            ?? throw new InvalidOperationException("Message body is not a SalesInvoicePostedEvent.");

        _logger.LogInformation("Invoice {InvoiceNumber} posted (event {EventId}, delivery {Delivery}, resend {Resend})",
            evt.InvoiceNumber, evt.EventId, message.DeliveryCount, evt.IsResend);

        var container = _blobs.GetBlobContainerClient(PdfContainer);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var pdfBlob = container.GetBlobClient($"{evt.InvoiceNumber}.pdf");
        var sentMarker = container.GetBlobClient($"{evt.InvoiceNumber}.emailed");

        // ---- idempotency: already done? ----
        if (!evt.IsResend && (await sentMarker.ExistsAsync(cancellationToken)).Value)
        {
            _logger.LogInformation("Invoice {InvoiceNumber} was already emailed. Skipping duplicate delivery.", evt.InvoiceNumber);
            return;
        }

        // ---- 1. PDF from the API, archived in Blob Storage ----
        var pdf = await _api.GetInvoicePdfAsync(evt.InvoiceId, cancellationToken);

        await pdfBlob.UploadAsync(
            new BinaryData(pdf),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" } },
            cancellationToken);

        // ---- 2. Who gets it? ----
        var customer = await _api.GetCustomerAsync(evt.CustomerId, cancellationToken);

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            // Not an error worth retrying: there is nobody to send to.
            _logger.LogWarning("Customer {CustomerId} has no email address. PDF archived, nothing sent.", evt.CustomerId);
            return;
        }

        // ---- 3. Send ----
        await _email.SendAsync(
            customer.Email,
            $"Invoice {evt.InvoiceNumber}",
            $"Dear {customer.CustomerName}, your invoice {evt.InvoiceNumber} for {evt.TotalAmount:N2} is attached.",
            pdfBlob.Uri.ToString(),
            cancellationToken);

        // ---- 4. Remember that it was sent ----
        await sentMarker.UploadAsync(
            new BinaryData(JsonSerializer.Serialize(new { evt.EventId, SentAtUtc = DateTime.UtcNow })),
            overwrite: true,
            cancellationToken);
    }
}
