namespace CoreLibs.LiveConfig;

/// <summary>
/// Fetches configuration data from an external source (e.g. Google Sheets, JSON files, API).
/// </summary>
public interface IConfigSource
{
    /// <summary>
    /// Unique name identifying this source.
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Fetches a snapshot for a specific configuration type.
    /// </summary>
    Task<ConfigSnapshot> FetchAsync(string configType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches snapshots for multiple configuration types in a single batched operation.
    /// Sources that support batching (e.g. Google Sheets) should override this to fetch
    /// all required data in one request rather than making per-type calls.
    /// </summary>
    async Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchManyAsync(
        IReadOnlyCollection<string> configTypes, CancellationToken cancellationToken = default)
    {
        var snapshots = new Dictionary<string, ConfigSnapshot>(configTypes.Count);
        foreach (var configType in configTypes)
            snapshots[configType] = await FetchAsync(configType, cancellationToken);
        return snapshots;
    }

    /// <summary>
    /// Fetches a snapshot for a specific configuration type, returning <c>null</c> if the source
    /// cannot produce one (e.g. missing file, unavailable sheet, first-time run).
    /// The default implementation wraps <see cref="FetchAsync"/> in a try/catch.
    /// </summary>
    async Task<ConfigSnapshot?> FetchOrDefaultAsync(string configType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await FetchAsync(configType, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fetches snapshots for all configuration types this source provides.
    /// </summary>
    Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchAllAsync(CancellationToken cancellationToken = default);
}
