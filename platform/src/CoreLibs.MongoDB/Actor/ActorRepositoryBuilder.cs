using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreLibs.MongoDB.Actor;

/// <summary>
/// Fluent builder returned by <c>AddActorRepository</c> to configure cache, factory,
/// and finalizer registrations for an actor repository.
/// </summary>
public class ActorRepositoryBuilder<TRepository, TEntity, TActor, TActorResult>(
    IServiceCollection services)
    where TRepository : class, IActorRepository<TEntity, TActor, TActorResult>
    where TEntity : EntityCas
    where TActor : Actor<TEntity>, TActorResult
    where TActorResult : IActorResult<TEntity>
{
    public ActorRepositoryBuilder<TRepository, TEntity, TActor, TActorResult> WithCache<TCache>()
        where TCache : class, IEntityCache<TEntity>
    {
        services.AddSingleton<IEntityCache<TEntity>, TCache>();
        return this;
    }

    public ActorRepositoryBuilder<TRepository, TEntity, TActor, TActorResult> WithFactory<TFactory>()
        where TFactory : class, IActorFactory<TEntity, TActor>
    {
        services.TryAddSingleton<IActorFactory<TEntity, TActor>, TFactory>();
        return this;
    }

    public ActorRepositoryBuilder<TRepository, TEntity, TActor, TActorResult> WithFinalizer<TFinalizer>()
        where TFinalizer : class, IActorFinalizer<TEntity, TActorResult>
    {
        services.TryAddSingleton<IActorFinalizer<TEntity, TActorResult>, TFinalizer>();
        return this;
    }
}
