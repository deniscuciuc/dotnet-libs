using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig.Json;

/// <summary>
/// File-based config source that reads JSON files from a directory.
/// Supports environment-based layering: {type}.json → {type}.{env}.json.
/// </summary>
[ConfigSource("json-file")]
public sealed class JsonFileConfigSource(
    JsonFileConfigSourceOptions options,
    ILogger<JsonFileConfigSource> logger) : IConfigSource
{
    public string SourceName => "json-file";

    public Task<ConfigSnapshot> FetchAsync(string configType, CancellationToken cancellationToken = default)
    {
        var json = ReadConfigFile(configType)
                   ?? throw new InvalidOperationException($"Config file not found for type '{configType}'");
        var hash = ConfigHasher.ComputeHash(json);

        return Task.FromResult(
            new ConfigSnapshot(configType, json, hash, DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchAllAsync(
        CancellationToken cancellationToken = default)
    {
        var snapshots = new Dictionary<string, ConfigSnapshot>();
        var dir = options.Directory;

        if (!Directory.Exists(dir))
        {
            logger.LogWarning("Config directory '{Directory}' does not exist", dir);
            return Task.FromResult<IReadOnlyDictionary<string, ConfigSnapshot>>(snapshots);
        }

        var files = Directory.GetFiles(dir, options.SearchPattern);
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            if (!string.IsNullOrEmpty(options.Environment) &&
                fileName.EndsWith($".{options.Environment}", StringComparison.OrdinalIgnoreCase))
                continue;

            if (fileName.Contains('.'))
                continue;

            var json = ReadConfigFile(fileName);
            if (json is null) continue;

            var hash = ConfigHasher.ComputeHash(json);
            snapshots[fileName] = new ConfigSnapshot(fileName, json, hash, DateTimeOffset.UtcNow);
        }

        logger.LogInformation("Loaded {Count} config snapshots from directory '{Directory}'", snapshots.Count, dir);
        return Task.FromResult<IReadOnlyDictionary<string, ConfigSnapshot>>(snapshots);
    }

    private string? ReadConfigFile(string configType)
    {
        var baseFile = Path.Combine(options.Directory, $"{configType}.json");

        if (!File.Exists(baseFile))
        {
            logger.LogDebug("Config file not found: {Path}", baseFile);
            return null;
        }

        var json = File.ReadAllText(baseFile);

        if (string.IsNullOrEmpty(options.Environment)) return json;

        var envFile = Path.Combine(options.Directory, $"{configType}.{options.Environment}.json");
        if (!File.Exists(envFile)) return json;

        json = File.ReadAllText(envFile);
        logger.LogDebug("Using environment override for {ConfigType}: {Path}", configType, envFile);

        return json;
    }
}
