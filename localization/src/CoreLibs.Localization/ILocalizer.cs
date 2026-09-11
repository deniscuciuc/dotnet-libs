namespace CoreLibs.Localization;

/// <summary>
/// Main entry point for localizing strings at runtime.
/// </summary>
public interface ILocalizer
{
    /// <summary>
    /// Returns the localized string for <paramref name="key"/> in <paramref name="culture"/>,
    /// interpolating any named placeholders from <paramref name="args"/>.
    /// </summary>
    string Get(LocalizationKey key, LanguageCode culture, LocalizationArgs args = default);

    /// <summary>
    /// Returns the correctly pluralized localized string for <paramref name="key"/> based on
    /// <paramref name="count"/>. The count is automatically available as <c>{count}</c> in the
    /// template; additional values can be supplied via <paramref name="args"/>.
    /// </summary>
    string GetPlural(LocalizationKey key, LanguageCode culture, long count, LocalizationArgs args = default);
}
