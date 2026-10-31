using System.Text.Json;

namespace TSQR.Common.Messaging;

/// <summary>
/// Outbox-backed implementation of <see cref="ICrossDomainEventBus"/>.
/// Publishing serializes the event and writes it to the outbox; the actual
/// transport call is made later by a background publisher.
/// </summary>
public sealed class OutboxCrossDomainEventBus(IOutboxStore outboxStore) : ICrossDomainEventBus
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType = integrationEvent.GetType();
        var payload = JsonSerializer.Serialize(integrationEvent, eventType, JsonOptions);
        var message = new OutboxMessage(
            integrationEvent.EventId,
            eventType.FullName ?? eventType.Name,
            payload,
            integrationEvent.OccurredOn);

        return outboxStore.EnqueueAsync(message, cancellationToken);
    }
}
