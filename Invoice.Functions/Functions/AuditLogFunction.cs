using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Invoice.Events;
using Invoice.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Invoice.Functions.Functions;

/// <summary>
/// TRIGGER: subscription "audit-log". This subscription has NO filter, so it receives EVERY event
/// (invoice posted, PO approved, receipt posted ...). Each one is stored as a JSON blob:
///   audit-log/2026/11/05/SalesInvoicePosted-{eventId}.json
///
/// A third subscriber on the same topic, written without touching the API or the other functions:
/// that is the point of publish / subscribe.
/// </summary>
public sealed class AuditLogFunction
{
    private readonly BlobServiceClient _blobs;
    private readonly ILogger<AuditLogFunction> _logger;

    public AuditLogFunction(BlobServiceClient blobs, ILogger<AuditLogFunction> logger)
    {
        _blobs = blobs;
        _logger = logger;
    }

    [Function(nameof(AuditLogFunction))]
    public async Task Run(
        [ServiceBusTrigger(ServiceBusNames.Topic, ServiceBusNames.AuditLogSubscription, Connection = ServiceBusClientFactory.ConnectionName)]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        var eventType = message.Subject
            ?? (message.ApplicationProperties.TryGetValue(ServiceBusNames.EventTypeProperty, out var value) ? value?.ToString() : null)
            ?? "Unknown";

        var container = _blobs.GetBlobContainerClient("audit-log");
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var when = message.EnqueuedTime.UtcDateTime;
        var name = $"{when:yyyy}/{when:MM}/{when:dd}/{eventType}-{message.MessageId}.json";

        // Overwrite = idempotent: a redelivered message just rewrites the same blob.
        await container.GetBlobClient(name).UploadAsync(message.Body, overwrite: true, cancellationToken);

        _logger.LogInformation("Audit: stored {EventType} as {Blob}", eventType, name);
    }
}
