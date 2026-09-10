using System.Text.Json;
using CoreLibs.LiveConfig;
using CoreLibs.LiveConfig.GSheet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.Localization.LiveConfig.GSheet;

public sealed class LocalizationGSheetConfigSource(
    GSheetImportOrchestrator orchestrator,
    GSheetImportContext context,
    IOptions<LocalizationOptions> localizationOptions,
    ILogger<LocalizationGSheetConfigSource> logger) : IConfigSource
{
    public string SourceName => LocalizationConstants.GSheetSourceName;

    public async Task<ConfigSnapshot> FetchAsync(
        string configType, CancellationToken cancellationToken = default)
    {
        var all = await FetchAllAsync(cancellationToken);
        if (all.TryGetValue(configType, out var snapshot))
            return snapshot;

        throw new InvalidOperationException(
            $"Config type '{configType}' not found after GSheet import. " +
            $"Available: [{string.Join(", ", all.Keys)}]");
    }

    public async Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchManyAsync(
        IReadOnlyCollection<string> configTypes, CancellationToken cancellationToken = default)
    {
        var all = await FetchAllAsync(cancellationToken);
        return all
            .Where(kv => configTypes.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public async Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching localization snapshots from Google Sheets");

        await orchestrator.RunSpecificImporterAsync<LocalizationGSheetImporter>(cancellationToken);

        var locSnapshots = context.GetDomain<LocalizationSnapshot>()?.ToList() ?? [];

        if (locSnapshots.Count == 0)
        {
            var configuredCultures = GetConfiguredCultures(localizationOptions.Value);
            if (configuredCultures.Count == 0)
            {
                logger.LogWarning("No localization snapshots produced from Google Sheets");
                return new Dictionary<string, ConfigSnapshot>();
            }

            logger.LogInformation(
                "No localization rows were found in Google Sheets. Emitting empty snapshots for configured cultures: {Cultures}",
                string.Join(", ", configuredCultures));

            locSnapshots = configuredCultures
                .Select(culture => new LocalizationSnapshot
                {
                    Culture = culture,
                    Entries = new Dictionary<string, LocalizationValue>()
                })
                .ToList();
        }

        var result = new Dictionary<string, ConfigSnapshot>(locSnapshots.Count);
        foreach (var locSnapshot in locSnapshots)
        {
            var configType = LocalizationConstants.ConfigType(locSnapshot.Culture);
            var json = JsonSerializer.Serialize(locSnapshot, JsonSerializerOptions.Web);
            var hash = LocalizationConstants.ComputeHash(json);
            result[configType] = new ConfigSnapshot(configType, json, hash, DateTimeOffset.UtcNow);

            logger.LogDebug("Produced config snapshot for {ConfigType} ({Count} entries)",
                configType, locSnapshot.Entries.Count);
        }

        logger.LogInformation("Localization GSheet source produced {Count} config snapshot(s)", result.Count);
        return result;
    }

    private static IReadOnlyList<string> GetConfiguredCultures(LocalizationOptions options)
    {
        return options.FallbackChain
            .Prepend(options.DefaultCulture)
            .Where(culture => !string.IsNullOrWhiteSpace(culture))
            .Select(culture => culture.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
