namespace TSQR.Common.Events;

/// <summary>
/// Handles a domain event after the originating transaction has committed.
/// Use this for slow or best-effort work such as notifications, email, or
/// integration-event publication.
/// </summary>
/// <remarks>
/// Post-commit handlers must not mutate the aggregates from the originating
/// command and must be idempotent: the transaction has already committed when
/// they run.
/// </remarks>
public interface IPostCommitDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    ValueTask HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
