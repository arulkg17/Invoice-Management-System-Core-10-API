using Invoice.Events;

namespace Invoice.CqrsAPI.Messaging;

/// <summary>
/// Announces an integration event to the outside world (Azure Service Bus topic).
/// Contract: this method NEVER throws. The database change has already been committed when it is called,
/// so a broker outage must not turn a successful command into an error response.
/// (The price of that: an event can be lost. The Outbox pattern, the next lesson, fixes it.)
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
