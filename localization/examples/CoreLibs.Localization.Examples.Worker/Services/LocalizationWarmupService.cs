using CoreLibs.Localization;
using CoreLibs.Localization.LiveConfig.Json;

namespace CoreLibs.Localization.Examples.Worker.Services;

/// <summary>
/// Startup service that seeds the in-memory cache from locale JSON files.
/// Runs once before the application begins serving requests.
/// Files are expected at: {ContentRoot}/locales/{culture}.json
/// </summary>
public sealed class LocalizationWarmupService(
    LocalizationJsonLoader loader,
    ILocalizationCache cache,
    ILogger<LocalizationWarmupService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        var loaded = 0;
        foreach (var snapshot in loader.LoadAll(LocalizationConstants.DefaultLocalesDirectory))
        {
            cache.Update(snapshot.Culture, snapshot.Entries);
            loaded++;
            logger.LogInformation(
                "Locale warmup: loaded {Count} entries for culture '{Culture}' from JSON.",
                snapshot.Entries.Count, snapshot.Culture);
        }

        if (loaded == 0)
            logger.LogWarning("Locale warmup: no .json files found in '{Directory}/' directory.",
                LocalizationConstants.DefaultLocalesDirectory);
        else
            logger.LogInformation("Locale warmup complete — {Total} culture(s) loaded.", loaded);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
