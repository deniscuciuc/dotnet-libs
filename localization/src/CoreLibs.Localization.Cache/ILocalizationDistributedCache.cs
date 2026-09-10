namespace CoreLibs.Localization.Cache;

/// <summary>
/// Optional Redis distributed cache for localization data.
/// Provides cross-instance sync and fast cold-start.
/// </summary>
public interface ILocalizationDistributedCache
{
    Task<IReadOnlyDictionary<string, LocalizationValue>?> GetAsync(
        string culture, CancellationToken ct = default);

    Task SetAsync(
        string culture,
        IReadOnlyDictionary<string, LocalizationValue> entries,
        CancellationToken ct = default);

    Task InvalidateAsync(string culture, CancellationToken ct = default);
}
