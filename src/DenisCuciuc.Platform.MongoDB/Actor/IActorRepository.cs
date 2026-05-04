namespace DenisCuciuc.Platform.MongoDB.Actor;

public interface IActorRepository<TEntity, TActor>
    : IActorRepository<TEntity, TActor, IActorResult<TEntity>>
    where TEntity : EntityCas
    where TActor : Actor<TEntity>, IActorResult<TEntity>;

public interface IActorRepository<TEntity, TActor, TActorResult> : IRepository<TEntity>
    where TEntity : EntityCas
    where TActor : Actor<TEntity>, TActorResult
    where TActorResult : IActorResult<TEntity>
{
    ActorOperations<TEntity, TActor, TActorResult> Actor { get; }
}
