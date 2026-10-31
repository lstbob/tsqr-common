namespace TSQR.Common.Messaging;

/// <summary>
/// Transport adapter used by the outbox background publisher. Implement this
/// with the service's HTTP client, broker producer, or message bus.
/// </summary>
public interface IIntegrationEventTransport
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
