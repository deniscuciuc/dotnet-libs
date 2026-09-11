using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig;

/// <summary>
/// Generic applier that deserializes JSON into a list and updates
/// a corresponding <see cref="IConfigCache{T}"/>.
/// </summary>
public sealed class CacheConfigApplier<T>(
    IConfigCache<IReadOnlyList<T>> cache,
    string configType,
    ILogger<CacheConfigApplier<T>> logger) : IConfigApplier where T : class
{
    public string ConfigType => configType;

    public Task ApplyAsync(string json, int version, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Deserialize<List<T>>(json, ConfigJsonOptions.Default);
        if (data is null)
        {
            logger.LogWarning("Deserialized null for config type {ConfigType} v{Version}", ConfigType, version);
            return Task.CompletedTask;
        }

        cache.Update(data.AsReadOnly(), version);
        logger.LogInformation("Applied config {ConfigType} v{Version} with {Count} items", ConfigType, version,
            data.Count);

        return Task.CompletedTask;
    }
}
