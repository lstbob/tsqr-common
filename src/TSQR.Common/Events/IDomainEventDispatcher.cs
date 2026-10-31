namespace TSQR.Common.Events;

/// <summary>
/// Dispatches collected domain events to their registered handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
