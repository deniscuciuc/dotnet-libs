using CoreLibs.Localization;
using Microsoft.Extensions.Options;

namespace CoreLibs.Localization.Examples.Worker.Services;

/// <summary>
/// Startup service that seeds the in-memory cache from the MongoDB persistent store.
/// Useful after a restart when LiveConfig is not yet available to push fresh data.
/// Cultures are derived from <see cref="LocalizationOptions.FallbackChain"/> + <see cref="LocalizationOptions.DefaultCulture"/>.
/// </summary>
public sealed class LocalizationStoreWarmupService(
    ILocalizationStore store,
    ILocalizationCache cache,
    IOptions<LocalizationOptions> options,
    ILogger<LocalizationStoreWarmupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        // Ensure store schema exists (no-op for MongoDB, creates table for PostgreSQL).
        await store.EnsureCreatedAsync(ct);

        var cultures = options.Value.FallbackChain
            .Prepend(options.Value.DefaultCulture)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var culture in cultures)
        {
            var entries = (await store.FindAllAsync(culture, ct)).ToList();

            if (entries.Count == 0)
            {
                logger.LogInformation("Store warmup: no entries found for culture '{Culture}'. Skipping.", culture);
                continue;
            }

            cache.Update(culture, entries.ToDictionary(e => e.Key, e => e.Value));
            logger.LogInformation(
                "Store warmup: loaded {Count} entries for culture '{Culture}'.",
                entries.Count, culture);
        }
    }

    public Task StopAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
