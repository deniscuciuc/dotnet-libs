using CoreLibs.Localization;

namespace CoreLibs.Localization.Store.MongoDB;

/// <summary>
/// MongoDB entity for a localization entry.
/// </summary>
public sealed class MongoLocalizationEntity
{
    public string Id { get; set; } = null!; // "{culture}:{key}"
    public string Key { get; set; } = null!;
    public string Culture { get; set; } = null!;
    public LocalizationValue Value { get; set; } = null!;
}
