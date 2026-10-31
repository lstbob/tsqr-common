namespace TSQR.Common.Messaging;

/// <summary>
/// Durable store for integration events. An implementation must write the
/// event in the same transaction as the aggregate change that produced it.
/// </summary>
public interface IOutboxStore
{
    Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OutboxMessage>> DequeueAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid eventId,
        string error,
        CancellationToken cancellationToken = default);
}
