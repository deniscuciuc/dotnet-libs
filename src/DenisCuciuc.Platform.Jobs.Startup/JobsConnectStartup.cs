using DenisCuciuc.Platform.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.Jobs.Startup;

/// <summary>
/// Startup task that validates the job scheduler is available and operational.
/// </summary>
public sealed class JobsConnectStartup : IStartup
{
    public int Order => 0;

    public Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();
        var scheduler = scope.ServiceProvider.GetService<IJobScheduler>();

        if (scheduler is null)
        {
            host.Logger.LogWarning(
                "No IJobScheduler registered. Ensure AddPlatformJobs() was called with a valid provider configuration");
            return Task.CompletedTask;
        }

        host.Logger.LogInformation(
            "Job scheduler ready: {SchedulerType}", scheduler.GetType().Name);

        host.MarkConfigured<IJobScheduler>();
        return Task.CompletedTask;
    }
}
