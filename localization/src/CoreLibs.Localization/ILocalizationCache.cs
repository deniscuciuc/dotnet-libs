namespace CoreLibs.Localization;

/// <summary>
/// Thread-safe in-memory store for all localization data, keyed by culture.
/// Updated atomically when LiveConfig pushes a new version.
/// </summary>
public interface ILocalizationCache
{
    /// <summary>Returns a provider for the given culture, or null if not loaded.</summary>
    ILocalizationProvider? GetProvider(string culture);

    /// <summary>Replaces the full dataset for a culture atomically.</summary>
    void Update(string culture, IReadOnlyDictionary<string, LocalizationValue> entries);

    /// <summary>Returns all cultures currently loaded.</summary>
    IReadOnlyList<string> GetLoadedCultures();
}
