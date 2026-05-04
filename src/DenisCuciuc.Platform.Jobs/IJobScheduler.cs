namespace DenisCuciuc.Platform.Jobs;

/// <summary>
/// Unified abstraction for scheduling background, delayed and recurring jobs.
/// </summary>
public interface IJobScheduler
{
    /// <summary>Enqueues a job for immediate execution.</summary>
    Task EnqueueAsync<TJob>(CancellationToken ct = default) where TJob : IPlatformJob;

    /// <summary>Enqueues a job for immediate execution with arguments.</summary>
    Task EnqueueAsync<TJob>(object args, CancellationToken ct = default) where TJob : IPlatformJob;

    /// <summary>Schedules a job to execute after the specified delay.</summary>
    Task ScheduleAsync<TJob>(TimeSpan delay, CancellationToken ct = default) where TJob : IPlatformJob;

    /// <summary>Schedules a job to execute after the specified delay with arguments.</summary>
    Task ScheduleAsync<TJob>(object args, TimeSpan delay, CancellationToken ct = default) where TJob : IPlatformJob;

    /// <summary>Adds or updates a recurring job with the given cron expression.</summary>
    Task AddOrUpdateRecurringAsync<TJob>(string jobId, string cron, CancellationToken ct = default) where TJob : IPlatformJob;

    /// <summary>Removes a recurring job.</summary>
    Task RemoveRecurringAsync(string jobId, CancellationToken ct = default);

    /// <summary>Triggers an existing recurring job immediately.</summary>
    Task TriggerRecurringAsync(string jobId, CancellationToken ct = default);
}
