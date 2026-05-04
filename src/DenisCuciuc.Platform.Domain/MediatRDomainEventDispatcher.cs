using MediatR;

namespace DenisCuciuc.Platform.Domain;

/// <summary>
/// Dispatches domain events via MediatR's <see cref="IPublisher"/>.
/// Events are dequeued from each entity and published in order.
/// </summary>
public sealed class MediatRDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchEventsAsync(Entity entity, CancellationToken cancellationToken = default)
    {
        var events = entity.DequeueDomainEvents();

        foreach (var domainEvent in events)
            await publisher.Publish(domainEvent, cancellationToken);
    }

    public async Task DispatchEventsAsync(IEnumerable<Entity> entities, CancellationToken cancellationToken = default)
    {
        var allEvents = new List<IDomainEvent>();

        foreach (var entity in entities)
            allEvents.AddRange(entity.DequeueDomainEvents());

        foreach (var domainEvent in allEvents)
            await publisher.Publish(domainEvent, cancellationToken);
    }
}
