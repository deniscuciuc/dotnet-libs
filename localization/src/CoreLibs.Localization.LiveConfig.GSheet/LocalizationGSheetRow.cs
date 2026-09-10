namespace CoreLibs.Localization.LiveConfig.GSheet;

/// <summary>
/// Row parsed from Google Sheet.
/// Format: Key | ru | en | de | ...
/// </summary>
public sealed class LocalizationGSheetRow
{
    public string Key { get; set; } = null!;
    public Dictionary<string, string> Translations { get; set; } = [];
}
