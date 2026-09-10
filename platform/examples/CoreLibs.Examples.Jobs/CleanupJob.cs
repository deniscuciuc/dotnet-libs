using CoreLibs.Jobs;

namespace CoreLibs.Examples.Jobs;

[CoreJob(Retries = 1)]
public sealed class CleanupJob(ILogger<CleanupJob> logger) : ICoreJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running daily cleanup...");
        return Task.CompletedTask;
    }
}
