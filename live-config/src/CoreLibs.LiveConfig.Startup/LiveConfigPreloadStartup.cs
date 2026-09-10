using CoreLibs.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.LiveConfig.Startup;

/// <summary>
/// CoreLibs startup task that preloads critical config types before the application
/// starts accepting traffic. Runs during <c>RunWithStartupAsync()</c>.
/// </summary>
public sealed class LiveConfigPreloadStartup : IStartup
{
    /// <summary>
    /// Runs after infrastructure (MongoDB, Redis) is connected.
    /// </summary>
    public int Order => 100;

    public async Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        host.Logger.LogInformation("Resolving LiveConfig preload options");
        var opts = host.Services.GetRequiredService<IOptions<LiveConfigOptions>>().Value;
        host.Logger.LogInformation("Resolved LiveConfig preload options");

        if (!opts.EnablePreload || opts.CriticalConfigTypes.Count == 0)
        {
            host.Logger.LogInformation("LiveConfig preload disabled — configs will be loaded on demand");
            host.Services.GetRequiredService<LiveConfigConsumerState>().SetReady();
            host.MarkConfigured<LiveConfigEngine>();
            return;
        }

        host.Logger.LogInformation("Resolving LiveConfig preloader");
        var preloader = host.Services.GetRequiredService<LiveConfigPreloader>();
        host.Logger.LogInformation("Resolved LiveConfig preloader");

        host.Logger.LogInformation("LiveConfig preloading {Count} critical config types",
            opts.CriticalConfigTypes.Count);

        await preloader.PreloadAsync(opts.CriticalConfigTypes, opts.Retry, opts.PreloadSourceName,
            opts.ForceSourceRefresh, cancellationToken);

        host.Logger.LogInformation("LiveConfig preload completed");
        host.Services.GetRequiredService<LiveConfigConsumerState>().SetReady();
        host.MarkConfigured<LiveConfigEngine>();
    }
}
