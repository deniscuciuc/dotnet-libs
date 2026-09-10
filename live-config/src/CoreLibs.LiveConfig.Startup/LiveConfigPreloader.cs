using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig.Startup;

/// <summary>
/// Preloads critical configs from the active store/distributor and, if missing,
/// bootstraps them from a registered config source.
/// </summary>
public sealed class LiveConfigPreloader(
    LiveConfigEngine engine,
    ConfigSourceRegistry sourceRegistry,
    ILogger<LiveConfigPreloader> logger)
{
    public async Task PreloadAsync(
        IReadOnlyCollection<string> configTypes,
        RetryOptions retry,
        string? preloadSourceName = null,
        bool forceSourceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (forceSourceRefresh)
        {
            await BatchForceRefreshAsync(configTypes, retry, preloadSourceName, cancellationToken);
            return;
        }

        // Non-force mode: try loading each from store, bootstrap missing ones in batch
        var missingConfigTypes = new List<string>();

        foreach (var configType in configTypes)
        {
            logger.LogInformation("Preloading config type: {ConfigType}", configType);

            if (await engine.LoadAndApplyAsync(configType, cancellationToken))
            {
                logger.LogInformation("Preloaded config type {ConfigType} from active store/distributor", configType);
                continue;
            }

            missingConfigTypes.Add(configType);
        }

        if (missingConfigTypes.Count > 0)
            await BatchBootstrapAsync(missingConfigTypes, retry, preloadSourceName, cancellationToken);
    }

    private async Task BatchForceRefreshAsync(
        IReadOnlyCollection<string> configTypes,
        RetryOptions retry,
        string? preloadSourceName,
        CancellationToken cancellationToken)
    {
        var source = ResolveBootstrapSource(preloadSourceName)
                     ?? throw new InvalidOperationException(
                         "ForceSourceRefresh is enabled but no config source is registered for startup bootstrap.");

        logger.LogInformation(
            "Force-refreshing {Count} config types from source {SourceName} in a single batch",
            configTypes.Count, source.SourceName);

        var snapshots = await FetchManyWithRetryAsync(source, configTypes, retry, cancellationToken);

        foreach (var configType in configTypes)
        {
            if (!snapshots.TryGetValue(configType, out var snapshot))
            {
                logger.LogWarning("Source {SourceName} did not return a snapshot for {ConfigType}",
                    source.SourceName, configType);
                continue;
            }

            var result = await engine.ImportRawAsync(configType, snapshot.DataJson, "startup-preload", cancellationToken);

            if (!result.IsSuccess)
                throw new InvalidOperationException(
                    $"Failed to import '{configType}' from source '{source.SourceName}': {result.Error ?? "unknown error"}");

            logger.LogInformation(
                "Preloaded config type {ConfigType} from source {SourceName} at v{Version}",
                configType, source.SourceName, result.Version);
        }
    }

    private async Task BatchBootstrapAsync(
        IReadOnlyCollection<string> configTypes,
        RetryOptions retry,
        string? preloadSourceName,
        CancellationToken cancellationToken)
    {
        var source = ResolveBootstrapSource(preloadSourceName);
        if (source is null)
            throw new InvalidOperationException(
                $"No active config found for [{string.Join(", ", configTypes)}] and no config source is registered for startup bootstrap.");

        logger.LogInformation(
            "Bootstrapping {Count} missing config types from source {SourceName} in a single batch",
            configTypes.Count, source.SourceName);

        var snapshots = await FetchManyWithRetryAsync(source, configTypes, retry, cancellationToken);

        foreach (var configType in configTypes)
        {
            if (!snapshots.TryGetValue(configType, out var snapshot))
            {
                logger.LogWarning("Source {SourceName} did not return a snapshot for {ConfigType}",
                    source.SourceName, configType);
                continue;
            }

            var result = await engine.ImportRawAsync(configType, snapshot.DataJson, "startup-preload", cancellationToken);

            if (!result.IsSuccess)
                throw new InvalidOperationException(
                    $"Failed to bootstrap '{configType}' from source '{source.SourceName}': {result.Error ?? "unknown error"}");

            logger.LogInformation("Bootstrapped config type {ConfigType} from source {SourceName} at v{Version}",
                configType, source.SourceName, result.Version);
        }
    }

    private async Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchManyWithRetryAsync(
        IConfigSource source,
        IReadOnlyCollection<string> configTypes,
        RetryOptions retry,
        CancellationToken cancellationToken)
    {
        var delay = retry.InitialDelay;

        for (var attempt = 0; attempt <= retry.MaxRetries; attempt++)
            try
            {
                return await source.FetchManyAsync(configTypes, cancellationToken);
            }
            catch (Exception ex) when (attempt < retry.MaxRetries)
            {
                logger.LogWarning(ex,
                    "Failed to batch-fetch configs, attempt {Attempt}/{Max}. Retrying in {Delay}ms",
                    attempt + 1, retry.MaxRetries, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(
                    Math.Min(delay.TotalMilliseconds * 2, retry.MaxDelay.TotalMilliseconds));
            }

        return await source.FetchManyAsync(configTypes, cancellationToken);
    }

    private IConfigSource? ResolveBootstrapSource(string? preloadSourceName)
    {
        if (!string.IsNullOrWhiteSpace(preloadSourceName))
            return sourceRegistry.Resolve(preloadSourceName)
                   ?? throw new InvalidOperationException(
                       $"Configured preload source '{preloadSourceName}' is not registered.");

        var sources = sourceRegistry.All.ToArray();
        return sources.Length switch
        {
            0 => null,
            1 => sources[0],
            _ => throw new InvalidOperationException(
                $"Multiple config sources are registered for startup bootstrap ({string.Join(", ", sourceRegistry.SourceNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}). " +
                $"Set '{LiveConfigOptions.SectionPath}:preloadSourceName' to choose one.")
        };
    }
}
