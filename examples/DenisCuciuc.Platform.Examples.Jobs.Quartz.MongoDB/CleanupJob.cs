using DenisCuciuc.Platform.Jobs;

namespace DenisCuciuc.Platform.Examples.Jobs.Quartz.MongoDB;

[PlatformJob(Retries = 1)]
public sealed class CleanupJob(ILogger<CleanupJob> logger) : IPlatformJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running daily cleanup...");
        return Task.CompletedTask;
    }
}
