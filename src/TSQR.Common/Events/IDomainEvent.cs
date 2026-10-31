namespace TSQR.Common.Events;

/// <summary>
/// Marker contract for an event raised by a domain aggregate.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC timestamp for when the event was created.</summary>
    DateTime OccurredOn { get; }
}

/// <summary>
/// Convenience base record for domain events. Captures the creation time once.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
