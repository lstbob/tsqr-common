using TSQR.Common.DomainKernel;
using TSQR.Common.Events;

namespace TSQR.Common.DomainKernel.Tests;

public sealed class DomainKernelTests
{
    [Fact]
    public void Entity_Equality_UsesIdentityAndRuntimeType()
    {
        var first = new TestEntity(new TestValueId(7));
        var second = new TestEntity(new TestValueId(7));
        var differentId = new TestEntity(new TestValueId(8));
        var differentType = new OtherTestEntity(new TestValueId(7));

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());

        Assert.False(first.Equals(differentId));
        Assert.False(first.Equals(differentType));

        var sameReference = first;
        Assert.True(first == sameReference);
    }

    [Fact]
    public void Entity_NullAndForeignKeyComparisonsAreSafe()
    {
        var entity = new TestEntity(new TestValueId(1));

        Assert.False(entity.Equals(null));
        Assert.False(entity.Equals("not an entity"));
        Assert.False(entity == null);
        Assert.False(null == entity);
        Assert.True(entity != null);
    }

    [Fact]
    public void Entity_IntegerIds_AreTransientWhenNonPositive()
    {
        Assert.True(new TestEntity(new TestValueId(0)).IsTransient());
        Assert.True(new TestEntity(new TestValueId(-1)).IsTransient());
        Assert.False(new TestEntity(new TestValueId(1)).IsTransient());
    }

    [Fact]
    public void Entity_TransientInstancesAreNotEqualEvenWithSameZeroId()
    {
        var first = new TestEntity(new TestValueId(0));
        var second = new TestEntity(new TestValueId(0));

        Assert.NotEqual(first, second);
        Assert.False(first == second);
    }

    [Fact]
    public void Entity_StructIds_UseDefaultForTransientDetection()
    {
        var transient = new StructIdEntity(default);
        var persisted = new StructIdEntity(new StructId(Guid.NewGuid()));
        var samePersistedId = new StructIdEntity(persisted.Id);

        Assert.True(transient.IsTransient());
        Assert.False(persisted.IsTransient());
        Assert.Equal(persisted, samePersistedId);
        Assert.False(persisted == transient);
    }

    [Fact]
    public void Entity_DomainEvents_CanBeAddedRemovedAndCleared()
    {
        var entity = new TestEntity(new TestValueId(1));
        var first = new TestDomainEvent(DateTime.UtcNow);
        var second = new TestDomainEvent(DateTime.UtcNow);

        entity.AddDomainEvent(first);
        entity.AddDomainEvent(second);
        Assert.Equal(new IDomainEvent[] { first, second }, entity.DomainEvents);

        entity.RemoveDomainEvent(first);
        Assert.Equal(new IDomainEvent[] { second }, entity.DomainEvents);

        entity.ClearDomainEvents();
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void Entity_RejectsNullDomainEvents()
    {
        var entity = new TestEntity(new TestValueId(1));

        Assert.Throws<ArgumentNullException>(() => entity.AddDomainEvent(null!));
    }

    [Fact]
    public void Entity_PersistenceHooks_AssignRowVersionAndId()
    {
        var entity = new TestEntity(new TestValueId(0));

        entity.SetRowVersion(42);
        entity.SetAssignedId(new TestValueId(99));

        Assert.Equal(42, entity.RowVersion);
        Assert.Equal(99, entity.Id.Value);
        Assert.False(entity.IsTransient());
    }

    [Fact]
    public void ValueObject_Equality_UsesComponentsAndRuntimeType()
    {
        var first = new TestValueId(7);
        var second = new TestValueId(7);
        var different = new TestValueId(8);
        var differentType = new OtherValueId(7);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());

        Assert.False(first.Equals(different));
        Assert.False(first.Equals(differentType));
    }

    [Fact]
    public void ValueObject_GetCopy_IsEqualButNotSameReference()
    {
        var original = new TestValueId(7);
        var copy = original.GetCopy();

        Assert.Equal(original, copy);
        Assert.NotSame(original, copy);
    }

    private sealed class TestEntity(TestValueId id) : Entity<TestValueId>(id);

    private sealed class OtherTestEntity(TestValueId id) : Entity<TestValueId>(id);

    private sealed class StructIdEntity(StructId id) : Entity<StructId>(id);

    private sealed class TestValueId(int value) : ValueObject, IIntegerId
    {
        public int Value => value;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }

    private sealed class OtherValueId(int value) : ValueObject
    {
        public int Value => value;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }

    private readonly struct StructId(Guid value) : IEquatable<StructId>
    {
        public Guid Value { get; } = value;

        public bool Equals(StructId other) => Value == other.Value;

        public override bool Equals(object? obj) =>
            obj is StructId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(StructId left, StructId right) =>
            left.Equals(right);

        public static bool operator !=(StructId left, StructId right) =>
            !left.Equals(right);
    }

    private sealed record TestDomainEvent(DateTime OccurredOn) : IDomainEvent;
}
