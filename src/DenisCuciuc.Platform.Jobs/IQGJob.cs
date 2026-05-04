namespace DenisCuciuc.Platform.Jobs;

/// <summary>
/// Contract for a background job.
/// Implementations are resolved from DI as scoped services.
/// </summary>
public interface IPlatformJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Contract for a background job that receives typed arguments.
/// </summary>
public interface IPlatformJob<TArgs> : IPlatformJob
{
    TArgs? Args { get; set; }
}
