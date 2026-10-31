namespace TSQR.Common.Messaging;

/// <summary>
/// Contract for an event that crosses a bounded context / service boundary.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Stable identifier used for idempotency and deduplication.</summary>
    Guid EventId { get; }

    /// <summary>UTC timestamp for when the event was created.</summary>
    DateTime OccurredOn { get; }
}

/// <summary>
/// Convenience base record for integration events.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
