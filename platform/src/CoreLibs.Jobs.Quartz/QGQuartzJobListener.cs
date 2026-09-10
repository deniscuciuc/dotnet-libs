using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Listener;

namespace CoreLibs.Jobs.Quartz;

/// <summary>
/// Quartz job listener that provides logging, metrics, and correlation ID propagation.
/// </summary>
internal sealed class CoreQuartzJobListener(ILoggerFactory loggerFactory) : JobListenerSupport
{
    private static readonly Meter Meter = new("CoreLibs.Jobs", "1.0.0");

    private static readonly Histogram<double> JobDuration =
        Meter.CreateHistogram<double>("CoreLibs.jobs.duration", "ms", "Job execution duration in milliseconds");

    private static readonly Counter<long> JobsExecuted =
        Meter.CreateCounter<long>("CoreLibs.jobs.executed", "jobs", "Total jobs executed");

    private static readonly Counter<long> JobsFailed =
        Meter.CreateCounter<long>("CoreLibs.jobs.failed", "jobs", "Total jobs failed");

    private const string StopwatchKey = "CoreLibs-Stopwatch";

    private readonly ILogger _logger = loggerFactory.CreateLogger("CoreLibs.Jobs.Quartz");

    public override string Name => "CoreLibs.JobListener";

    public override Task JobToBeExecuted(IJobExecutionContext context, CancellationToken ct = default)
    {
        var jobName = context.JobDetail.Key.Name;
        _logger.LogInformation("Job {JobName} starting", jobName);

        context.Put(StopwatchKey, Stopwatch.StartNew());

        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        Activity.Current?.SetTag("qgcore.correlation_id", correlationId);
        Activity.Current?.SetBaggage("correlation-id", correlationId);

        return Task.CompletedTask;
    }

    public override Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException,
        CancellationToken ct = default)
    {
        var jobName = context.JobDetail.Key.Name;
        var tags = new TagList { { "job.type", jobName } };

        if (context.Get(StopwatchKey) is Stopwatch sw)
        {
            sw.Stop();
            JobDuration.Record(sw.Elapsed.TotalMilliseconds, tags);
        }

        JobsExecuted.Add(1, tags);

        if (jobException is not null)
        {
            JobsFailed.Add(1, tags);
            _logger.LogError(jobException, "Job {JobName} failed", jobName);
        }
        else
        {
            _logger.LogInformation("Job {JobName} completed", jobName);
        }

        return Task.CompletedTask;
    }
}
