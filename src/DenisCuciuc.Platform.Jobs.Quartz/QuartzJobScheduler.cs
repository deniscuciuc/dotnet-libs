using Microsoft.Extensions.Logging;
using Quartz;

namespace DenisCuciuc.Platform.Jobs.Quartz;

/// <summary>
/// <see cref="IJobScheduler"/> implementation backed by Quartz.NET.
/// </summary>
public sealed class QuartzJobScheduler(
    ISchedulerFactory schedulerFactory,
    ILogger<QuartzJobScheduler> logger) : IJobScheduler
{
    private const string JobGroup = "platform";

    public async Task EnqueueAsync<TJob>(CancellationToken ct) where TJob : IPlatformJob
    {
        logger.LogDebug("Enqueuing job {JobType}", typeof(TJob).Name);
        var scheduler = await schedulerFactory.GetScheduler(ct);

        var job = CreateJobDetail<TJob>(Guid.NewGuid().ToString("N"));
        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger, ct);
    }

    public async Task EnqueueAsync<TJob>(object args, CancellationToken ct) where TJob : IPlatformJob
    {
        logger.LogDebug("Enqueuing job {JobType} with args", typeof(TJob).Name);
        var scheduler = await schedulerFactory.GetScheduler(ct);

        var job = CreateJobDetail<TJob>(Guid.NewGuid().ToString("N"), args);
        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger, ct);
    }

    public async Task ScheduleAsync<TJob>(TimeSpan delay, CancellationToken ct) where TJob : IPlatformJob
    {
        logger.LogDebug("Scheduling job {JobType} with delay {Delay}", typeof(TJob).Name, delay);
        var scheduler = await schedulerFactory.GetScheduler(ct);

        var job = CreateJobDetail<TJob>(Guid.NewGuid().ToString("N"));
        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .StartAt(DateTimeOffset.UtcNow.Add(delay))
            .Build();

        await scheduler.ScheduleJob(job, trigger, ct);
    }

    public async Task ScheduleAsync<TJob>(object args, TimeSpan delay, CancellationToken ct) where TJob : IPlatformJob
    {
        logger.LogDebug("Scheduling job {JobType} with delay {Delay} and args", typeof(TJob).Name, delay);
        var scheduler = await schedulerFactory.GetScheduler(ct);

        var job = CreateJobDetail<TJob>(Guid.NewGuid().ToString("N"), args);
        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .StartAt(DateTimeOffset.UtcNow.Add(delay))
            .Build();

        await scheduler.ScheduleJob(job, trigger, ct);
    }

    public async Task AddOrUpdateRecurringAsync<TJob>(string jobId, string cron, CancellationToken ct)
        where TJob : IPlatformJob
    {
        var quartzCron = ToQuartzCron(cron);
        logger.LogInformation("Adding/updating recurring job {JobId} ({JobType}) cron={Cron}",
            jobId, typeof(TJob).Name, quartzCron);

        var scheduler = await schedulerFactory.GetScheduler(ct);
        var jobKey = new JobKey(jobId, JobGroup);

        var job = JobBuilder.Create<QuartzJobAdapter<TJob>>()
            .WithIdentity(jobKey)
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{jobId}-trigger", JobGroup)
            .ForJob(jobKey)
            .WithCronSchedule(quartzCron)
            .Build();

        if (await scheduler.CheckExists(jobKey, ct)) await scheduler.DeleteJob(jobKey, ct);

        await scheduler.ScheduleJob(job, trigger, ct);
    }

    /// <summary>
    /// Converts a standard 5-field Unix cron expression to the 6-field Quartz format.
    /// Quartz requires a leading seconds field and does not allow both day-of-month
    /// and day-of-week to be <c>*</c> simultaneously.
    /// If the expression already has 6+ fields it is returned unchanged.
    /// </summary>
    private static string ToQuartzCron(string cron)
    {
        var parts = cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        string[] quartz;

        if (parts.Length >= 6)
        {
            quartz = parts[..6];
        }
        else
        {
            // 5-field Unix -> 6-field Quartz: prepend seconds=0
            quartz = new string[6];
            quartz[0] = "0";
            Array.Copy(parts, 0, quartz, 1, parts.Length);
        }

        // Quartz: day-of-month (index 3) and day-of-week (index 5) cannot both be specified.
        // One of them must be ? (don't care). Skip if already has a ? in either position.
        if (quartz[3] != "?" && quartz[5] != "?")
        {
            if (quartz[3] == "*" && quartz[5] != "*")
                quartz[3] = "?"; // DOW is specific → DOM must be ?
            else
                quartz[5] = "?"; // DOM is specific or both * → DOW must be ?
        }

        return string.Join(" ", quartz);
    }

    public async Task RemoveRecurringAsync(string jobId, CancellationToken ct)
    {
        logger.LogInformation("Removing recurring job {JobId}", jobId);
        var scheduler = await schedulerFactory.GetScheduler(ct);
        await scheduler.DeleteJob(new JobKey(jobId, JobGroup), ct);
    }

    public async Task TriggerRecurringAsync(string jobId, CancellationToken ct)
    {
        logger.LogInformation("Triggering recurring job {JobId}", jobId);
        var scheduler = await schedulerFactory.GetScheduler(ct);
        await scheduler.TriggerJob(new JobKey(jobId, JobGroup), ct);
    }

    private static IJobDetail CreateJobDetail<TJob>(string identity, object? args = null) where TJob : IPlatformJob
    {
        var builder = JobBuilder.Create<QuartzJobAdapter<TJob>>()
            .WithIdentity(identity, JobGroup);

        if (args is not null)
            builder.UsingJobData(QuartzJobAdapter<TJob>.ArgsKey,
                System.Text.Json.JsonSerializer.Serialize(args));

        return builder.Build();
    }
}
