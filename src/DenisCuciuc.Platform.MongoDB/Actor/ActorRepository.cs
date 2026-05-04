using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.MongoDB.Actor;

/// <summary>
/// Convenience base using <see cref="IActorResult{TEntity}"/> as the default result type.
/// </summary>
public abstract class ActorRepository<TEntity, TActor>(
    IServiceProvider serviceProvider,
    IMongoDBProvider provider,
    IndexesBuilder<TEntity> indexes,
    IEntityCache<TEntity> cache,
    IActorFactory<TEntity, TActor> factory,
    IActorFinalizer<TEntity, IActorResult<TEntity>> finalizer,
    ILogger logger)
    : ActorRepository<TEntity, TActor, IActorResult<TEntity>>(
        serviceProvider, provider, indexes, cache, factory, finalizer, logger)
    where TEntity : EntityCas
    where TActor : Actor<TEntity>, IActorResult<TEntity>;

/// <summary>
/// Repository with built-in CAS actor operations.
/// Extends <see cref="EntityRepository{TEntity}"/> so all standard CRUD, indexes, and counters remain available.
/// </summary>
public abstract class ActorRepository<TEntity, TActor, TActorResult>
    : EntityRepository<TEntity>, IActorRepository<TEntity, TActor, TActorResult>
    where TEntity : EntityCas
    where TActor : Actor<TEntity>, TActorResult
    where TActorResult : IActorResult<TEntity>
{
    protected ActorRepository(
        IServiceProvider serviceProvider,
        IMongoDBProvider provider,
        IndexesBuilder<TEntity> indexes,
        IEntityCache<TEntity> cache,
        IActorFactory<TEntity, TActor> factory,
        IActorFinalizer<TEntity, TActorResult>? finalizer,
        ILogger logger)
        : base(provider, indexes, logger)
    {
        Actor = new ActorOperations<TEntity, TActor, TActorResult>(
            serviceProvider, this, cache, factory, finalizer);
    }

    public ActorOperations<TEntity, TActor, TActorResult> Actor { get; }
}
