namespace CoreLibs.MongoDB.Actor;

public interface IActorFactory<in TEntity, out TActor>
    where TEntity : EntityCas
    where TActor : Actor<TEntity>
{
    TActor Create(TEntity entity, ActorOwner? owner);
}
