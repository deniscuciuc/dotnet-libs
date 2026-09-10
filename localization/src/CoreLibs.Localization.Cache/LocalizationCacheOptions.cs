namespace CoreLibs.Localization.Cache;

public sealed class LocalizationCacheOptions
{
    public const string SectionPath = "Localization:Cache";

    /// <summary>Redis key prefix. Default: "localization".</summary>

    public string KeyPrefix { get; set; } = "localization";

    /// <summary>TTL for cached entries. Default: 1 hour.</summary>

    public TimeSpan Expiry { get; set; } = TimeSpan.FromHours(1);
}
