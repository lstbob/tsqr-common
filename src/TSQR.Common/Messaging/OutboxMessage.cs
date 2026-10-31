namespace TSQR.Common.Messaging;

/// <summary>
/// Persisted integration-event envelope. The event id is also the idempotency
/// key used by consumers.
/// </summary>
public sealed record OutboxMessage(
    Guid EventId,
    string EventType,
    string Payload,
    DateTime OccurredOn,
    int Attempts = 0,
    string? LastError = null);
