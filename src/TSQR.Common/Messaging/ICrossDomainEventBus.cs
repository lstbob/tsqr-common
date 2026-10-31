namespace TSQR.Common.Messaging;

/// <summary>
/// Publishes integration events across bounded contexts. Implementations are
/// expected to provide transport-specific concerns such as serialization,
/// retries, and a transactional outbox.
/// </summary>
public interface ICrossDomainEventBus
{
    Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;
}

/// <summary>
/// Handles an integration event received from another bounded context.
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : class, IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
