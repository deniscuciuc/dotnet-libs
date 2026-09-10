namespace CoreLibs.Localization;

/// <summary>
/// Core options for the CoreLibs Localization framework.
/// </summary>
public sealed class LocalizationOptions
{
    public const string SectionPath = "Localization";

    /// <summary>
    /// Default fallback culture when the requested culture has no translation.
    /// Default: "en".
    /// </summary>

    public string DefaultCulture { get; set; } = "en";

    public List<string> FallbackChain { get; set; } = ["en"];

    /// <summary>
    /// When true, missing keys are logged as warnings. Default: true.
    /// </summary>

    public bool LogMissingKeys { get; set; } = true;

    /// <summary>
    /// When a key is missing even after fallback, return the key itself instead of empty string.
    /// Default: true.
    /// </summary>

    public bool ReturnKeyOnMissing { get; set; } = true;

    /// <summary>
    /// Custom plural rules per culture code, keyed by ISO 639-1 code (e.g. "tr", "zh", "ja").
    /// Entries here take precedence over the built-in CLDR rule table.
    /// Use this to add unsupported cultures or override built-in behaviour.
    /// The function receives the count and must return a CLDR category name:
    /// "zero", "one", "two", "few", "many", "other".
    /// </summary>
    public Dictionary<string, Func<long, string>> PluralRules { get; set; } = [];
}
