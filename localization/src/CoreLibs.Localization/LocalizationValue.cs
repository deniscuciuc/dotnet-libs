namespace CoreLibs.Localization;

/// <summary>
/// Represents a single localized value — a string with optional plural forms.
/// </summary>
public sealed class LocalizationValue
{
    /// <summary>The default string value.</summary>
    public string Value { get; set; } = null!;

    /// <summary>
    /// Optional plural forms keyed by plural category (e.g. "one", "few", "many", "other").
    /// </summary>
    public Dictionary<string, string>? Plurals { get; set; }
}
