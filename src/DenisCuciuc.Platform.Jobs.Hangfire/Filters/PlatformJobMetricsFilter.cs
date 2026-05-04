using System.Diagnostics;
using System.Diagnostics.Metrics;
using Hangfire.Common;
using Hangfire.Server;

namespace DenisCuciuc.Platform.Jobs.Hangfire.Filters;

/// <summary>
/// Emits OpenTelemetry metrics for job execution (duration, count, failures).
/// </summary>
internal sealed class PlatformJobMetricsFilter : JobFilterAttribute, IServerFilter
{
    private static readonly Meter Meter = new("DenisCuciuc.Platform.Jobs", "1.0.0");

    private static readonly Histogram<double> JobDuration =
        Meter.CreateHistogram<double>("deniscuciuc.platform.jobs.duration", "ms", "Job execution duration in milliseconds");

    private static readonly Counter<long> JobsExecuted =
        Meter.CreateCounter<long>("deniscuciuc.platform.jobs.executed", "jobs", "Total jobs executed");

    private static readonly Counter<long> JobsFailed =
        Meter.CreateCounter<long>("deniscuciuc.platform.jobs.failed", "jobs", "Total jobs failed");

    private const string StopwatchKey = "Platform-Stopwatch";

    public void OnPerforming(PerformingContext context)
    {
        context.Items[StopwatchKey] = Stopwatch.StartNew();
    }

    public void OnPerformed(PerformedContext context)
    {
        var jobName = context.BackgroundJob.Job?.Method?.Name ?? "Unknown";
        var tags = new TagList { { "job.type", jobName } };

        if (context.Items.TryGetValue(StopwatchKey, out var obj) && obj is Stopwatch sw)
        {
            sw.Stop();
            JobDuration.Record(sw.Elapsed.TotalMilliseconds, tags);
        }

        JobsExecuted.Add(1, tags);

        if (context.Exception is not null)
            JobsFailed.Add(1, tags);
    }
}
