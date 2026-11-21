namespace TSQR.Common.DomainKernel;

/// <summary>
/// Base class for value objects. Equality is based on the components returned
/// by <see cref="GetEqualityComponents"/> and the concrete runtime type.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Gets the components that define equality for this value object.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
            hash.Add(component);

        return hash.ToHashCode();
    }

    /// <summary>
    /// Creates a shallow copy of the value object.
    /// </summary>
    public ValueObject GetCopy() => (ValueObject)MemberwiseClone();

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        EqualOperator(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !EqualOperator(left, right);

    private static bool EqualOperator(ValueObject? left, ValueObject? right)
    {
        if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
            return false;

        return ReferenceEquals(left, null) || left.Equals(right);
    }
}
