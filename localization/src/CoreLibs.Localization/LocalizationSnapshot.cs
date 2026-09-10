namespace CoreLibs.Localization;

/// <summary>
/// Snapshot of all localization entries for a single culture,
/// produced by LiveConfig sources (GSheet, JSON).
/// </summary>
public sealed class LocalizationSnapshot
{
    /// <summary>Culture code, e.g. "ru", "en".</summary>
    public string Culture { get; set; } = null!;

    /// <summary>All localized entries for this culture, keyed by namespace.key.</summary>
    public Dictionary<string, LocalizationValue> Entries { get; set; } = [];
}
