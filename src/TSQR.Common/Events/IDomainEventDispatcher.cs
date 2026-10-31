namespace TSQR.Common.Events;

/// <summary>
/// Dispatches collected domain events to their registered handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches handlers that must run inside the current transaction.
    /// </summary>
    ValueTask DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches handlers that run after the transaction commits.
    /// </summary>
    ValueTask DispatchPostCommitAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
