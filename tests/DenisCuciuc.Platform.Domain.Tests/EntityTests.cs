namespace DenisCuciuc.Platform.Domain.Tests;

public class EntityTests
{
    [Fact]
    public void NewEntity_HasUniqueId()
    {
        var a = new TestEntity();
        var b = new TestEntity();

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void NewEntity_HasNonEmptyId()
    {
        var entity = new TestEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        var entity = new TestEntityWithId(Guid.NewGuid());
        var clone = new TestEntityWithId(entity.Id);

        Assert.True(entity.Equals(clone));
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var a = new TestEntity();
        var b = new TestEntity();

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        var entity = new TestEntity();
        var other = new OtherTestEntity();

        Assert.False(entity.Equals(other));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var entity = new TestEntity();

        Assert.False(entity.Equals(null));
    }

    [Fact]
    public void EqualityOperator_Works()
    {
        var id = Guid.NewGuid();
        var a = new TestEntityWithId(id);
        var b = new TestEntityWithId(id);

        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void GetHashCode_SameId_SameHash()
    {
        var id = Guid.NewGuid();
        var a = new TestEntityWithId(id);
        var b = new TestEntityWithId(id);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void AddDomainEvent_IncreasesCount()
    {
        var entity = new TestEntity();
        var evt = new TestDomainEvent();

        entity.AddDomainEvent(evt);

        Assert.Single(entity.DomainEvents);
        Assert.True(entity.HasDomainEvents);
    }

    [Fact]
    public void AddDomainEvent_NullThrows()
    {
        var entity = new TestEntity();

        Assert.Throws<ArgumentNullException>(() => entity.AddDomainEvent(null!));
    }

    [Fact]
    public void DequeueDomainEvents_ClearsAndReturns()
    {
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestDomainEvent());
        entity.AddDomainEvent(new TestDomainEvent());

        var events = entity.DequeueDomainEvents();

        Assert.Equal(2, events.Count);
        Assert.Empty(entity.DomainEvents);
        Assert.False(entity.HasDomainEvents);
    }

    [Fact]
    public void DequeueDomainEvents_EmptyReturnsEmpty()
    {
        var entity = new TestEntity();

        var events = entity.DequeueDomainEvents();

        Assert.Empty(events);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAll()
    {
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestDomainEvent());

        entity.ClearDomainEvents();

        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void UuidV7_IsTimeOrdered()
    {
        // UUIDv7 embeds a millisecond timestamp, so IDs created with a delay should be ordered
        var first = new TestEntity().Id;
        Thread.Sleep(2);
        var second = new TestEntity().Id;

        Assert.True(second > first, "Second UUIDv7 should be greater than first");
    }

    private class TestEntity : Entity;

    private class TestEntityWithId : Entity
    {
        public TestEntityWithId(Guid id) => Id = id;
    }

    private class OtherTestEntity : Entity;

    private record TestDomainEvent : DomainEvent;
}
