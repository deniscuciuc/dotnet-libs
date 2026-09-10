using Microsoft.Extensions.Logging;

namespace CoreLibs.Localization.Cache;

public sealed class DistributedLocalizationCacheDecorator : ILocalizationCache
{
    private readonly ILocalizationCache _inner;
    private readonly ILocalizationDistributedCache _distributed;
    private readonly ILogger<DistributedLocalizationCacheDecorator> _logger;

    public DistributedLocalizationCacheDecorator(
        ILocalizationCache inner,
        ILocalizationDistributedCache distributed,
        ILogger<DistributedLocalizationCacheDecorator> logger)
    {
        _inner = inner;
        _distributed = distributed;
        _logger = logger;
    }

    public ILocalizationProvider? GetProvider(string culture)
    {
        return _inner.GetProvider(culture);
    }

    public void Update(string culture, IReadOnlyDictionary<string, LocalizationValue> entries)
    {
        _inner.Update(culture, entries);

        // Write-through to distributed cache (fire-and-forget)
        _ = WriteThroughAsync(culture, entries);
    }

    public IReadOnlyList<string> GetLoadedCultures()
    {
        return _inner.GetLoadedCultures();
    }

    public async Task WarmupFromDistributedAsync(
        IEnumerable<string> cultures, CancellationToken ct = default)
    {
        foreach (var culture in cultures)
            try
            {
                var entries = await _distributed.GetAsync(culture, ct);
                if (entries is not null)
                {
                    _inner.Update(culture, entries);
                    _logger.LogInformation(
                        "Warmed up culture '{Culture}' from distributed cache ({Count} entries)",
                        culture, entries.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to warm up culture '{Culture}' from distributed cache", culture);
            }
    }

    private async Task WriteThroughAsync(
        string culture, IReadOnlyDictionary<string, LocalizationValue> entries)
    {
        try
        {
            await _distributed.SetAsync(culture, entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to write-through to distributed cache for culture '{Culture}'", culture);
        }
    }
}
