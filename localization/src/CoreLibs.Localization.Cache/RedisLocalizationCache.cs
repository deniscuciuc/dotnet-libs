using System.Text.Json;
using CoreLibs.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.Localization.Cache;

/// <summary>
/// Redis implementation of <see cref="ILocalizationDistributedCache"/>.
/// Stores locale data as JSON under key "localization:{culture}".
/// </summary>
public sealed class RedisLocalizationCache : ILocalizationDistributedCache
{
    private readonly IRedisClient _redis;
    private readonly LocalizationCacheOptions _options;
    private readonly ILogger<RedisLocalizationCache> _logger;

    public RedisLocalizationCache(
        IRedisClient redis,
        IOptions<LocalizationCacheOptions> options,
        ILogger<RedisLocalizationCache> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, LocalizationValue>?> GetAsync(
        string culture, CancellationToken ct = default)
    {
        var db = _redis.Database;
        var key = BuildKey(culture);
        var value = await db.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return null;
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, LocalizationValue>>(
                (string?)value ?? string.Empty, JsonSerializerOptions.Web);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize localization cache for culture '{Culture}'", culture);
            return null;
        }
    }

    public async Task SetAsync(
        string culture,
        IReadOnlyDictionary<string, LocalizationValue> entries,
        CancellationToken ct = default)
    {
        var db = _redis.Database;
        var key = BuildKey(culture);
        var json = JsonSerializer.Serialize(entries, JsonSerializerOptions.Web);
        await db.StringSetAsync(key, json, _options.Expiry);
    }

    public async Task InvalidateAsync(string culture, CancellationToken ct = default)
    {
        var db = _redis.Database;
        await db.KeyDeleteAsync(BuildKey(culture));
    }

    private string BuildKey(string culture)
    {
        return $"{_options.KeyPrefix}:{culture}";
    }
}
