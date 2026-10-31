namespace TSQR.Common.Events;

/// <summary>
/// Handles a single domain event type.
/// </summary>
/// <typeparam name="TEvent">The concrete domain event type.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
