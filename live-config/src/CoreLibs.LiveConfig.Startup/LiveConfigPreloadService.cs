using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.LiveConfig.Startup;

/// <summary>
/// Background service that handles the polling fallback and consumer-state lifecycle.
/// Preloading is handled by <see cref="LiveConfigPreloadStartup"/> during <c>RunWithStartupAsync()</c>.
/// When not using CoreLibs startup, this service also preloads critical configs.
/// </summary>
public sealed class LiveConfigPreloadService(
    LiveConfigEngine engine,
    LiveConfigPreloader preloader,
    LiveConfigConsumerState consumerState,
    IOptions<LiveConfigOptions> options,
    ILogger<LiveConfigPreloadService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;

        try
        {
            if (!consumerState.IsReady)
            {
                if (opts is { EnablePreload: true, CriticalConfigTypes.Count: > 0 })
                {
                    logger.LogInformation("LiveConfig preload starting for {Count} critical config types",
                        opts.CriticalConfigTypes.Count);

                    await preloader.PreloadAsync(opts.CriticalConfigTypes, opts.Retry, opts.PreloadSourceName,
                        opts.ForceSourceRefresh, stoppingToken);

                    logger.LogInformation("LiveConfig preload completed");
                }
                else
                {
                    logger.LogInformation("LiveConfig preload disabled — configs will be loaded on demand");
                }

                consumerState.SetReady();
            }

            if (opts.EnablePollingFallback)
            {
                logger.LogInformation("Polling fallback enabled with interval {Interval}", opts.PollingInterval);
                await PollLoopAsync(opts, stoppingToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "LiveConfig preload failed");
            if (!consumerState.IsReady)
                consumerState.SetFailed(ex);
        }
    }

    private async Task PollLoopAsync(LiveConfigOptions opts, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(opts.PollingInterval);

        while (await timer.WaitForNextTickAsync(ct))
            foreach (var configType in opts.CriticalConfigTypes)
                try
                {
                    await engine.LoadAndApplyAsync(configType, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Polling error for config type {ConfigType}", configType);
                }
    }
}
