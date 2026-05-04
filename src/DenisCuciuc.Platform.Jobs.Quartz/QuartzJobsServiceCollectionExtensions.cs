using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace DenisCuciuc.Platform.Jobs.Quartz;

public static class QuartzJobsServiceCollectionExtensions
{
    /// <summary>
    /// Registers Quartz.NET as the DenisCuciuc.Platform job scheduling provider.
    /// Called via reflection from <see cref="JobsServiceCollectionExtensions"/>.
    /// </summary>
    public static IServiceCollection AddQuartzJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddQuartz(q =>
        {
            q.UseInMemoryStore(); // default; overridden by store extensions (Postgres, MongoDB)
        });

        services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });

        // Register the job listener for observability
        services.AddSingleton<PlatformQuartzJobListener>();
        services.AddSingleton<ISchedulerListener>(sp => new QuartzListenerRegistrar(
            sp.GetRequiredService<ISchedulerFactory>(),
            sp.GetRequiredService<PlatformQuartzJobListener>()));

        services.AddScoped<QuartzJobScheduler>();
        services.AddScoped<IJobScheduler>(sp => sp.GetRequiredService<QuartzJobScheduler>());

        return services;
    }
}

/// <summary>
/// Registers the <see cref="PlatformQuartzJobListener"/> with the Quartz scheduler when initialized.
/// </summary>
internal sealed class QuartzListenerRegistrar(
    ISchedulerFactory schedulerFactory,
    PlatformQuartzJobListener jobListener) : ISchedulerListener
{
    private bool _registered;

    public async Task SchedulerStarted(CancellationToken ct = default)
    {
        if (_registered) return;
        _registered = true;

        var scheduler = await schedulerFactory.GetScheduler(ct);
        scheduler.ListenerManager.AddJobListener(jobListener);
    }

    // Required interface members â€” no-op
    public Task SchedulerError(string msg, SchedulerException cause, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task SchedulerInStandbyMode(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task SchedulerShutdown(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task SchedulerShuttingdown(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task SchedulingDataCleared(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task JobScheduled(ITrigger trigger, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task JobUnscheduled(TriggerKey triggerKey, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task TriggerFinalized(ITrigger trigger, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task TriggerPaused(TriggerKey triggerKey, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task TriggersPaused(string? triggerGroup, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task TriggerResumed(TriggerKey triggerKey, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task TriggersResumed(string? triggerGroup, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task JobAdded(IJobDetail jobDetail, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task JobDeleted(JobKey jobKey, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task JobPaused(JobKey jobKey, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task JobInterrupted(JobKey jobKey, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task JobsPaused(string jobGroup, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task JobResumed(JobKey jobKey, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task JobsResumed(string jobGroup, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task SchedulerStarting(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
