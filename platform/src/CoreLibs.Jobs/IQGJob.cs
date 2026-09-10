namespace CoreLibs.Jobs;

/// <summary>
/// Contract for a background job.
/// Implementations are resolved from DI as scoped services.
/// </summary>
public interface ICoreJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Contract for a background job that receives typed arguments.
/// </summary>
public interface ICoreJob<TArgs> : ICoreJob
{
    TArgs? Args { get; set; }
}
