namespace CoreLibs.MongoDB.Actor;

public interface IActorFinalizer<TEntity, in TActorResult>
    where TEntity : EntityCas
    where TActorResult : IActorResult<TEntity>
{
    bool CanHandle(TActorResult result);
    Task HandleAsync(TActorResult result);
}
