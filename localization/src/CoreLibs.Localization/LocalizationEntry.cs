namespace CoreLibs.Localization;

/// <summary>
/// A single localization entry — a key + culture + value triple.
/// </summary>
public sealed class LocalizationEntry
{
    public string Key { get; set; } = null!;
    public string Culture { get; set; } = null!;
    public LocalizationValue Value { get; set; } = null!;
}
