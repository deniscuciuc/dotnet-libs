using CoreLibs.Jobs;

namespace CoreLibs.Examples.Jobs.Hangfire.MongoDB;

[CoreJob(Retries = 3, TimeoutSeconds = 30)]
public sealed class SendEmailJob(ILogger<SendEmailJob> logger) : ICoreJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending email...");
        return Task.CompletedTask;
    }
}
