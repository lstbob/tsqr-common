using TSQR.Common.Events;

namespace TSQR.Common.DomainKernel;

/// <summary>
/// Non-generic base class for all domain entities. Holds the domain-event
/// collection so the application layer can dispatch and clear events without
/// knowing the entity's identifier type.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Optimistic concurrency token for the persisted row. Relational
    /// repositories load it when the entity is rehydrated and require it to
    /// match before updating or deleting the row.
    /// </summary>
    public long RowVersion { get; private set; }

    /// <summary>
    /// The domain events raised by this entity since the last dispatch.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>Records a domain event on this entity for later dispatch.</summary>
    public void AddDomainEvent(IDomainEvent eventItem)
    {
        ArgumentNullException.ThrowIfNull(eventItem);
        _domainEvents.Add(eventItem);
    }

    /// <summary>Removes a previously recorded domain event.</summary>
    public void RemoveDomainEvent(IDomainEvent eventItem) =>
        _domainEvents.Remove(eventItem);

    /// <summary>Clears all collected domain events.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Records the row version returned by the persistence layer. Intended
    /// for mapping and repository code only.
    /// </summary>
    public void SetRowVersion(long rowVersion) => RowVersion = rowVersion;
}

/// <summary>
/// Base class for entities identified by a strongly typed identifier.
/// </summary>
/// <typeparam name="TId">
/// Identifier type. It may be a class, record, or struct; equality is based on
/// the identifier value and the concrete entity type.
/// </typeparam>
public abstract class Entity<TId>(TId id) : Entity
    where TId : notnull
{
    private TId _id = id;

    /// <summary>The entity identifier.</summary>
    public TId Id => _id;

    /// <summary>
    /// Assigns an identifier created by the persistence layer. Intended for
    /// mapping and repository code only.
    /// </summary>
    public void SetAssignedId(TId id) => _id = id;

    /// <summary>
    /// Determines whether the entity has not been persisted yet. Integer-backed
    /// identifiers are transient while their value is non-positive; other
    /// identifier types are transient when they equal their default value.
    /// </summary>
    public bool IsTransient()
    {
        if (Id is null)
            return true;

        if (Id is IIntegerId integerId)
            return integerId.Value <= 0;

        return EqualityComparer<TId>.Default.Equals(Id, default!);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not Entity<TId> other)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (IsTransient() || other.IsTransient())
            return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override int GetHashCode() =>
        Id is null ? 0 : Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !Equals(left, right);
}
