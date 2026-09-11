using System.Reflection;
using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreLibs.Jobs.Hangfire;

/// <summary>
/// <see cref="IJobScheduler"/> implementation backed by Hangfire.
/// </summary>
public sealed class HangfireJobScheduler(
    IBackgroundJobClient jobClient,
    IRecurringJobManager recurringJobManager,
    IServiceScopeFactory scopeFactory,
    ILogger<HangfireJobScheduler> logger) : IJobScheduler
{
    public Task EnqueueAsync<TJob>(CancellationToken ct) where TJob : ICoreJob
    {
        logger.LogDebug("Enqueuing job {JobType}", typeof(TJob).Name);
        var typeName = typeof(TJob).AssemblyQualifiedName!;
        jobClient.Create(() => ExecuteJob(typeName, CancellationToken.None), new EnqueuedState());
        return Task.CompletedTask;
    }

    public Task EnqueueAsync<TJob>(object args, CancellationToken ct) where TJob : ICoreJob
    {
        logger.LogDebug("Enqueuing job {JobType} with args", typeof(TJob).Name);
        var typeName = typeof(TJob).AssemblyQualifiedName!;
        jobClient.Create(() => ExecuteJobWithArgs(typeName, args, CancellationToken.None), new EnqueuedState());
        return Task.CompletedTask;
    }

    public Task ScheduleAsync<TJob>(TimeSpan delay, CancellationToken ct) where TJob : ICoreJob
    {
        logger.LogDebug("Scheduling job {JobType} with delay {Delay}", typeof(TJob).Name, delay);
        var typeName = typeof(TJob).AssemblyQualifiedName!;
        jobClient.Create(() => ExecuteJob(typeName, CancellationToken.None), new ScheduledState(delay));
        return Task.CompletedTask;
    }

    public Task ScheduleAsync<TJob>(object args, TimeSpan delay, CancellationToken ct) where TJob : ICoreJob
    {
        logger.LogDebug("Scheduling job {JobType} with delay {Delay} and args", typeof(TJob).Name, delay);
        var typeName = typeof(TJob).AssemblyQualifiedName!;
        jobClient.Create(() => ExecuteJobWithArgs(typeName, args, CancellationToken.None), new ScheduledState(delay));
        return Task.CompletedTask;
    }

    public Task AddOrUpdateRecurringAsync<TJob>(string jobId, string cron, CancellationToken ct)
        where TJob : ICoreJob
    {
        logger.LogInformation("Adding/updating recurring job {JobId} ({JobType}) cron={Cron}",
            jobId, typeof(TJob).Name, cron);

        var queue = typeof(TJob).GetCustomAttribute<CoreJobAttribute>()?.Queue ?? "default";
        var typeName = typeof(TJob).AssemblyQualifiedName!;

        recurringJobManager.AddOrUpdate(
            jobId,
            queue,
            () => ExecuteJob(typeName, CancellationToken.None),
            cron);

        return Task.CompletedTask;
    }

    public Task RemoveRecurringAsync(string jobId, CancellationToken ct)
    {
        logger.LogInformation("Removing recurring job {JobId}", jobId);
        recurringJobManager.RemoveIfExists(jobId);
        return Task.CompletedTask;
    }

    public Task TriggerRecurringAsync(string jobId, CancellationToken ct)
    {
        logger.LogInformation("Triggering recurring job {JobId}", jobId);
        recurringJobManager.Trigger(jobId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Non-generic entry point invoked by Hangfire. Resolves the job from DI and executes it.
    /// Hangfire 1.8 cannot serialize generic methods in expression trees, so this uses a
    /// string-based type name that Hangfire can serialize/deserialize reliably.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteJob(string jobTypeAssemblyQualifiedName, CancellationToken ct)
    {
        var jobType = Type.GetType(jobTypeAssemblyQualifiedName)
                      ?? throw new InvalidOperationException(
                          $"Could not resolve job type '{jobTypeAssemblyQualifiedName}'.");

        using var scope = scopeFactory.CreateScope();
        var job = (ICoreJob)scope.ServiceProvider.GetRequiredService(jobType);
        await job.ExecuteAsync(ct);
    }

    /// <summary>
    /// Non-generic entry point with arguments support.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteJobWithArgs(string jobTypeAssemblyQualifiedName, object? args, CancellationToken ct)
    {
        var jobType = Type.GetType(jobTypeAssemblyQualifiedName)
                      ?? throw new InvalidOperationException(
                          $"Could not resolve job type '{jobTypeAssemblyQualifiedName}'.");

        using var scope = scopeFactory.CreateScope();
        var job = (ICoreJob)scope.ServiceProvider.GetRequiredService(jobType);

        if (args is not null && job is ICoreJob<object> typedJob)
            typedJob.Args = args;

        await job.ExecuteAsync(ct);
    }
}
