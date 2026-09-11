using System.Collections.Concurrent;

namespace CoreLibs.Jobs;

/// <summary>
/// In-memory implementation of <see cref="IJobScheduler"/> for unit testing.
/// Jobs are recorded but <b>not</b> executed.
/// </summary>
public sealed class InMemoryJobScheduler : IJobScheduler
{
    public ConcurrentQueue<EnqueuedJobRecord> EnqueuedJobs { get; } = new();
    public ConcurrentQueue<ScheduledJobRecord> ScheduledJobs { get; } = new();
    public ConcurrentDictionary<string, RecurringJobRecord> RecurringJobs { get; } = new();

    public Task EnqueueAsync<TJob>(CancellationToken ct = default) where TJob : ICoreJob
    {
        EnqueuedJobs.Enqueue(new EnqueuedJobRecord(typeof(TJob), null));
        return Task.CompletedTask;
    }

    public Task EnqueueAsync<TJob>(object args, CancellationToken ct = default) where TJob : ICoreJob
    {
        EnqueuedJobs.Enqueue(new EnqueuedJobRecord(typeof(TJob), args));
        return Task.CompletedTask;
    }

    public Task ScheduleAsync<TJob>(TimeSpan delay, CancellationToken ct = default) where TJob : ICoreJob
    {
        ScheduledJobs.Enqueue(new ScheduledJobRecord(typeof(TJob), null, delay));
        return Task.CompletedTask;
    }

    public Task ScheduleAsync<TJob>(object args, TimeSpan delay, CancellationToken ct = default)
        where TJob : ICoreJob
    {
        ScheduledJobs.Enqueue(new ScheduledJobRecord(typeof(TJob), args, delay));
        return Task.CompletedTask;
    }

    public Task AddOrUpdateRecurringAsync<TJob>(string jobId, string cron, CancellationToken ct = default)
        where TJob : ICoreJob
    {
        RecurringJobs[jobId] = new RecurringJobRecord(typeof(TJob), jobId, cron);
        return Task.CompletedTask;
    }

    public Task RemoveRecurringAsync(string jobId, CancellationToken ct = default)
    {
        RecurringJobs.TryRemove(jobId, out _);
        return Task.CompletedTask;
    }

    public Task TriggerRecurringAsync(string jobId, CancellationToken ct = default)
    {
        // No-op in test scheduler â€” just verify the job exists
        return Task.CompletedTask;
    }

    /// <summary>Clears all recorded jobs.</summary>
    public void Reset()
    {
        EnqueuedJobs.Clear();
        ScheduledJobs.Clear();
        RecurringJobs.Clear();
    }

    public sealed record EnqueuedJobRecord(Type JobType, object? Args);

    public sealed record ScheduledJobRecord(Type JobType, object? Args, TimeSpan Delay);

    public sealed record RecurringJobRecord(Type JobType, string JobId, string Cron);
}
