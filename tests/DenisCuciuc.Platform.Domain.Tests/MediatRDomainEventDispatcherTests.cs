using MediatR;
using NSubstitute;

namespace DenisCuciuc.Platform.Domain.Tests;

public class MediatRDomainEventDispatcherTests
{
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly MediatRDomainEventDispatcher _sut;

    public MediatRDomainEventDispatcherTests()
    {
        _sut = new MediatRDomainEventDispatcher(_publisher);
    }

    [Fact]
    public async Task DispatchEventsAsync_SingleEntity_PublishesAllEvents()
    {
        var entity = new TestEntity();
        var evt1 = new TestEvent("first");
        var evt2 = new TestEvent("second");
        entity.AddDomainEvent(evt1);
        entity.AddDomainEvent(evt2);

        await _sut.DispatchEventsAsync(entity);

        await _publisher.Received(1).Publish(evt1, Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(evt2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchEventsAsync_SingleEntity_ClearsEventsAfterDispatch()
    {
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestEvent("a"));

        await _sut.DispatchEventsAsync(entity);

        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public async Task DispatchEventsAsync_NoEvents_DoesNotPublish()
    {
        var entity = new TestEntity();

        await _sut.DispatchEventsAsync(entity);

        await _publisher.DidNotReceiveWithAnyArgs().Publish(Arg.Any<INotification>(), default);
    }

    [Fact]
    public async Task DispatchEventsAsync_MultipleEntities_PublishesAll()
    {
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        var evt1 = new TestEvent("from-1");
        var evt2 = new TestEvent("from-2");
        entity1.AddDomainEvent(evt1);
        entity2.AddDomainEvent(evt2);

        await _sut.DispatchEventsAsync([entity1, entity2]);

        await _publisher.Received(1).Publish(evt1, Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(evt2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchEventsAsync_MultipleEntities_ClearsAllEvents()
    {
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        entity1.AddDomainEvent(new TestEvent("a"));
        entity2.AddDomainEvent(new TestEvent("b"));

        await _sut.DispatchEventsAsync([entity1, entity2]);

        Assert.Empty(entity1.DomainEvents);
        Assert.Empty(entity2.DomainEvents);
    }

    [Fact]
    public async Task DispatchEventsAsync_PassesCancellationToken()
    {
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestEvent("x"));
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await _sut.DispatchEventsAsync(entity, token);

        await _publisher.Received(1).Publish(Arg.Any<TestEvent>(), token);
    }

    private sealed class TestEntity : Entity;

    private sealed record TestEvent(string Name) : DomainEvent;
}
