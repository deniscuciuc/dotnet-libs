using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.Jobs.Hangfire.Filters;

/// <summary>
/// Structured logging for job lifecycle events.
/// </summary>
internal sealed class PlatformJobLoggingFilter : JobFilterAttribute,
    IClientFilter, IServerFilter, IElectStateFilter
{
    public void OnCreating(CreatingContext context)
    {
    }

    public void OnCreated(CreatedContext context)
    {
    }

    public void OnPerforming(PerformingContext context)
    {
        var jobName = context.BackgroundJob.Job?.Method?.Name ?? "Unknown";
        var jobId = context.BackgroundJob.Id;

        using var factory = LoggerFactory.Create(b => b.AddConsole());
        var logger = factory.CreateLogger("DenisCuciuc.Platform.Jobs");
        logger.LogInformation("Job {JobId} ({JobName}) starting", jobId, jobName);
    }

    public void OnPerformed(PerformedContext context)
    {
        var jobId = context.BackgroundJob.Id;
        var jobName = context.BackgroundJob.Job?.Method?.Name ?? "Unknown";

        using var factory = LoggerFactory.Create(b => b.AddConsole());
        var logger = factory.CreateLogger("DenisCuciuc.Platform.Jobs");

        if (context.Exception is not null)
            logger.LogError(context.Exception,
                "Job {JobId} ({JobName}) failed", jobId, jobName);
        else
            logger.LogInformation("Job {JobId} ({JobName}) completed", jobId, jobName);
    }

    public void OnStateElection(ElectStateContext context)
    {
    }
}
