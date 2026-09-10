namespace CoreLibs.Localization;

/// <summary>
/// Provides read access to the current in-memory localization data for a single culture.
/// </summary>
public interface ILocalizationProvider
{
    /// <summary>The culture this provider covers (e.g. "ru", "en").</summary>
    string Culture { get; }

    /// <summary>Returns all entries for this culture.</summary>
    IReadOnlyDictionary<string, LocalizationValue> GetAll();

    /// <summary>Tries to find a value by key.</summary>
    bool TryGet(string key, out LocalizationValue? value);
}
