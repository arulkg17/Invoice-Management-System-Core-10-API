using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Invoice.Events;

namespace Invoice.CqrsAPI.Messaging;

/// <summary>Sends every event to the "ims-events" topic.</summary>
public sealed class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusEventPublisher> _logger;

    public ServiceBusEventPublisher(ServiceBusClient client, ILogger<ServiceBusEventPublisher> logger)
    {
        _sender = client.CreateSender(ServiceBusNames.Topic);
        _logger = logger;
    }

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Serialize the RUNTIME type, otherwise only the base-class properties would be written.
            var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType());

            var message = new ServiceBusMessage(body)
            {
                MessageId = integrationEvent.EventId.ToString(),   // duplicate detection
                Subject = integrationEvent.EventType,
                ContentType = "application/json",
                CorrelationId = integrationEvent.EventId.ToString()
            };

            // Subscriptions filter on this property: invoice-email only wants SalesInvoicePosted, and so on.
            message.ApplicationProperties[ServiceBusNames.EventTypeProperty] = integrationEvent.EventType;

            await _sender.SendMessageAsync(message, cancellationToken);

            _logger.LogInformation("Published {EventType} {EventId} to topic {Topic}",
                integrationEvent.EventType, integrationEvent.EventId, ServiceBusNames.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not publish {EventType} {EventId}. The business change is already saved; the event is lost.",
                integrationEvent.EventType, integrationEvent.EventId);
        }
    }

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}

/// <summary>Used when Service Bus is switched off (the default), so the API runs without any Azure resources.</summary>
public sealed class NoOpEventPublisher : IEventPublisher
{
    private readonly ILogger<NoOpEventPublisher> _logger;

    public NoOpEventPublisher(ILogger<NoOpEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Service Bus is disabled. {EventType} {EventId} was not published.",
            integrationEvent.EventType, integrationEvent.EventId);

        return Task.CompletedTask;
    }
}
