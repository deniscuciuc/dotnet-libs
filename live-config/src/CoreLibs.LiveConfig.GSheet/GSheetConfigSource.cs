using System.Reflection;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig.GSheet;

/// <summary>
/// Bridges GSheet orchestrator output into the LiveConfig <see cref="IConfigSource"/> pipeline.
/// After the orchestrator runs all importers, this source serializes the import context domains
/// and provides them as config snapshots keyed by config type.
/// </summary>
[ConfigSource("gsheet")]
public sealed class GSheetConfigSource(
    GSheetImportOrchestrator orchestrator,
    GSheetImportContext context,
    ILogger<GSheetConfigSource> logger) : IConfigSource
{
    public string SourceName => "gsheet";

    /// <summary>
    /// Fetches a specific config type by running the named importer and returning its domain data as JSON.
    /// </summary>
    public async Task<ConfigSnapshot> FetchAsync(string configType, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Fetching config type {ConfigType} from Google Sheets", configType);
            await orchestrator.RunSpecificImporterByNameAsync(configType, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run importer for config type {ConfigType}", configType);
            throw;
        }

        var importerType = GSheetImporterRegistry.ResolveImporterType(configType)
                           ?? throw new InvalidOperationException($"Importer not found for config type '{configType}'");

        var (_, domainType) = GetImporterGenericArgs(importerType);
        var domainData = context.GetDomain(domainType)
                         ?? throw new InvalidOperationException($"No domain data found for config type '{configType}'");

        var json = CanonicalJsonSerializer.Serialize(domainData, domainData.GetType());
        var hash = ConfigHasher.ComputeHash(json);
        logger.LogInformation("Fetched config type {ConfigType} from Google Sheets", configType);
        return new ConfigSnapshot(configType, json, hash, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Fetches multiple config types in a single batched GSheet request.
    /// All sheet ranges (including dependencies) are fetched in one batch,
    /// then importers run in dependency order against the cached data.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchManyAsync(
        IReadOnlyCollection<string> configTypes, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching {Count} config types from Google Sheets in a single batch", configTypes.Count);
        await orchestrator.RunMultipleImportersByNameAsync(configTypes, cancellationToken);

        var snapshots = new Dictionary<string, ConfigSnapshot>(configTypes.Count);
        foreach (var configType in configTypes)
        {
            var importerType = GSheetImporterRegistry.ResolveImporterType(configType)
                               ?? throw new InvalidOperationException(
                                   $"Importer not found for config type '{configType}'");

            var (_, domainType) = GetImporterGenericArgs(importerType);
            var domainData = context.GetDomain(domainType);
            if (domainData is null)
            {
                logger.LogWarning("No domain data found for config type {ConfigType} after batch import", configType);
                continue;
            }

            var json = CanonicalJsonSerializer.Serialize(domainData, domainData.GetType());
            var hash = ConfigHasher.ComputeHash(json);
            snapshots[configType] = new ConfigSnapshot(configType, json, hash, DateTimeOffset.UtcNow);
        }

        logger.LogInformation("Fetched {Count} config snapshots from Google Sheets", snapshots.Count);
        return snapshots;
    }

    /// <summary>
    /// Runs all importers and returns all domains as config snapshots.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching all config types from Google Sheets");
        await orchestrator.RunAsync(cancellationToken);

        var snapshots = new Dictionary<string, ConfigSnapshot>();
        var importerTypes = GSheetImporterRegistry.DiscoverImporterAndEntityTypes();

        foreach (var importerType in importerTypes)
        {
            var attr = importerType.GetCustomAttribute<GSheetImporterAttribute>();
            var configType = attr?.SheetName;
            if (configType is null)
            {
                var entityReg = GSheetImporterRegistry.GetStaticEntityRegistrations()
                    .FirstOrDefault(reg => reg.AdapterType == importerType);
                configType = entityReg?.SheetName;
            }

            if (string.IsNullOrWhiteSpace(configType))
                continue;

            var (_, domainType) = GetImporterGenericArgs(importerType);
            var domainData = context.GetDomain(domainType);
            if (domainData is null) continue;

            var json = CanonicalJsonSerializer.Serialize(domainData, domainData.GetType());
            var hash = ConfigHasher.ComputeHash(json);
            snapshots[configType] = new ConfigSnapshot(configType, json, hash, DateTimeOffset.UtcNow);
        }

        logger.LogInformation("Fetched {Count} config snapshots from Google Sheets", snapshots.Count);
        return snapshots;
    }

    private static (Type row, Type domain) GetImporterGenericArgs(Type importerType)
    {
        var i = importerType.GetInterfaces()
                    .FirstOrDefault(it =>
                        it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IGSheetPipeline<,>))
                ?? importerType.GetInterfaces()
                    .First(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IGSheetImporter<,>));
        var args = i.GetGenericArguments();
        return (args[0], args[1]);
    }
}
