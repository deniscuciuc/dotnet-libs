using DenisCuciuc.Platform.MongoDB.Exceptions;

namespace DenisCuciuc.Platform.MongoDB.Actor;

public class OperationResult<TActorResult, TEntity>(
    TActorResult actor,
    Exception? exception)
    where TActorResult : IActorResult<TEntity>
    where TEntity : EntityCas
{
    public TActorResult Actor { get; } = actor;
    public Exception? Exception { get; } = exception;
    public TEntity Entity => Actor.Entity;
    public bool Succeed => Exception == null;
    public bool EntityNotFound => Exception is EntityNotFoundException;
    public bool EntityDuplicated => Exception is EntityDuplicatedException;

    public static implicit operator bool(OperationResult<TActorResult, TEntity> value)
    {
        return value.Succeed;
    }
}
