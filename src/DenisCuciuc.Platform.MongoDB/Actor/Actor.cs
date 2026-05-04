using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.MongoDB.Actor;

/// <summary>
/// Base actor with pre/post processing lifecycle hooks.
/// </summary>
public abstract class Actor<TEntity>(
    TEntity entity,
    ActorContext context,
    ILogger logger) : IActorResult<TEntity>
    where TEntity : EntityCas
{
    public TEntity Entity { get; } = entity;

    public bool ActivatedByOwner
    {
        get
        {
            if (Context.Owner != null)
                return Context.Owner.Id == Entity.Id;

            return Entity.Id.ToString() == Context.Identity;
        }
    }

    protected ActorContext Context { get; } = context;
    protected ILogger Logger { get; } = logger;

    internal void PreProcessing()
    {
        OnPreProcessing();
    }

    internal void PostProcessing()
    {
        OnPostProcessing();
    }

    protected abstract void OnPreProcessing();
    protected abstract void OnPostProcessing();
}
