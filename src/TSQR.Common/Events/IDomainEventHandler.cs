namespace TSQR.Common.Events;

/// <summary>
/// Handles a single domain event type inside the current unit-of-work
/// transaction.
/// </summary>
/// <typeparam name="TEvent">The concrete domain event type.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    ValueTask HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
