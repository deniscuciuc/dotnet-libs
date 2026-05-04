using DenisCuciuc.Platform.Jobs;

namespace DenisCuciuc.Platform.Examples.Jobs.Quartz.MongoDB;

[PlatformJob(Retries = 3, TimeoutSeconds = 30)]
public sealed class SendEmailJob(ILogger<SendEmailJob> logger) : IPlatformJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending email...");
        return Task.CompletedTask;
    }
}
