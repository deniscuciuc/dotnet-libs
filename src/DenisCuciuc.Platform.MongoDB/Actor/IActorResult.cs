namespace DenisCuciuc.Platform.MongoDB.Actor;

public interface IActorResult<out TEntity>
    where TEntity : EntityCas
{
    bool ActivatedByOwner { get; }
    TEntity Entity { get; }
}
