using System.Text.Json;
using CoreLibs.LiveConfig;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace CoreLibs.Localization.LiveConfig.Json;

/// <summary>
/// Bridges <see cref="LocalizationJsonLoader"/> into the LiveConfig <see cref="IConfigSource"/> pipeline.
/// Reads locale JSON files (e.g. locales/ru.json) and produces <see cref="ConfigSnapshot"/> objects
/// keyed by config type <c>"localization:{culture}"</c>.
/// </summary>
public sealed class LocalizationJsonConfigSource(
    IFileProvider fileProvider,
    ILoggerFactory loggerFactory,
    string directory = LocalizationConstants.DefaultLocalesDirectory) : IConfigSource
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<LocalizationJsonConfigSource>();

    public string SourceName => LocalizationConstants.JsonSourceName;

    public async Task<ConfigSnapshot> FetchAsync(string configType, CancellationToken cancellationToken = default)
    {
        var all = await FetchAllAsync(cancellationToken);
        if (all.TryGetValue(configType, out var snapshot))
            return snapshot;

        throw new InvalidOperationException($"Config type '{configType}' not found in localization JSON source.");
    }

    public Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchAllAsync(
        CancellationToken cancellationToken = default)
    {
        var loader = new LocalizationJsonLoader(fileProvider, loggerFactory.CreateLogger<LocalizationJsonLoader>());
        var snapshots = new Dictionary<string, ConfigSnapshot>();

        foreach (var locSnapshot in loader.LoadAll(directory))
        {
            var configType = LocalizationConstants.ConfigType(locSnapshot.Culture);
            var json = JsonSerializer.Serialize(locSnapshot, JsonSerializerOptions.Web);
            var hash = LocalizationConstants.ComputeHash(json);
            snapshots[configType] = new ConfigSnapshot(configType, json, hash, DateTimeOffset.UtcNow);

            _logger.LogDebug("Produced config snapshot for {ConfigType} ({Count} entries)",
                configType, locSnapshot.Entries.Count);
        }

        _logger.LogInformation("Localization JSON source produced {Count} config snapshot(s) from '{Directory}'",
            snapshots.Count, directory);
        return Task.FromResult<IReadOnlyDictionary<string, ConfigSnapshot>>(snapshots);
    }
}
