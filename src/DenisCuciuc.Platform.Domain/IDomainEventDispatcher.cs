namespace DenisCuciuc.Platform.Domain;

/// <summary>
/// Dispatches domain events accumulated on <see cref="Entity"/> instances.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(Entity entity, CancellationToken cancellationToken = default);

    Task DispatchEventsAsync(IEnumerable<Entity> entities, CancellationToken cancellationToken = default);
}
