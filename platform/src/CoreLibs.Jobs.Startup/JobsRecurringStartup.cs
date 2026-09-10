using CoreLibs.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreLibs.Jobs.Startup;

/// <summary>
/// Startup task that registers recurring jobs declared at configuration time.
/// Runs after <see cref="JobsConnectStartup"/> to ensure the scheduler is ready.
/// </summary>
public sealed class JobsRecurringStartup : IStartup
{
    public int Order => 1;

    internal static readonly List<RecurringJobRegistration> Registrations = [];

    public async Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        if (Registrations.Count == 0)
        {
            host.Logger.LogDebug("No recurring jobs configured at startup");
            return;
        }

        using var scope = host.Services.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IJobScheduler>();

        host.Logger.LogInformation("Registering {Count} recurring job(s) ...", Registrations.Count);

        foreach (var reg in Registrations)
        {
            host.Logger.LogInformation(
                "Registering recurring job {JobId} ({JobType}) cron={Cron}",
                reg.JobId, reg.JobType.Name, reg.Cron);

            // Use reflection to call the generic AddOrUpdateRecurringAsync<TJob>
            var method = typeof(IJobScheduler)
                .GetMethod(nameof(IJobScheduler.AddOrUpdateRecurringAsync))!
                .MakeGenericMethod(reg.JobType);

            var task = (Task)method.Invoke(scheduler, [reg.JobId, reg.Cron, cancellationToken])!;
            await task;
        }
    }

    internal sealed record RecurringJobRegistration(Type JobType, string JobId, string Cron);
}
