namespace CoreLibs.Localization.Runtime;

internal static class PluralizationHelper
{
    private delegate string PluralRule(long n);

    /// <summary>
    /// Built-in CLDR rule table. Covers the most common language families.
    /// Unknown cultures fall back to Slavic rules for backward compatibility.
    /// Consumers can override or extend via <see cref="Core.LocalizationOptions.PluralRules"/>.
    /// </summary>
    private static readonly Dictionary<string, PluralRule> BuiltInRules = new(StringComparer.OrdinalIgnoreCase)
    {
        // Germanic: "one" for 1, "other" for everything else
        ["en"] = GetCategoryGermanic,
        ["de"] = GetCategoryGermanic,
        ["nl"] = GetCategoryGermanic,
        ["it"] = GetCategoryGermanic,
        ["es"] = GetCategoryGermanic,
        ["pt"] = GetCategoryGermanic,

        // Slavic: zero/one/few/many with mod10/mod100 rules
        ["ru"] = GetCategorySlavic,
        ["uk"] = GetCategorySlavic,
        ["pl"] = GetCategorySlavic,
        ["hr"] = GetCategorySlavic,
        ["sr"] = GetCategorySlavic,
        ["bs"] = GetCategorySlavic,

        // French: "one" for 0 and 1, "other" for rest
        ["fr"] = GetCategoryFrench,

        // Arabic: 6-form system
        ["ar"] = GetCategoryArabic
    };

    /// <param name="plurals">Plural forms defined for a key.</param>
    /// <param name="count">The numeric count to select a form for.</param>
    /// <param name="culture">Culture code used to pick the correct plural rule.</param>
    /// <param name="customRules">
    /// Optional consumer-defined overrides from <see cref="Core.LocalizationOptions.PluralRules"/>.
    /// Checked before the built-in table — allows adding unsupported cultures or patching existing ones.
    /// </param>
    public static string? SelectForm(
        Dictionary<string, string> plurals,
        long count,
        string? culture = null,
        IReadOnlyDictionary<string, Func<long, string>>? customRules = null)
    {
        var category = GetCategory(count, culture, customRules);
        if (plurals.TryGetValue(category, out var form))
            return form;
        return plurals.TryGetValue("other", out var other) ? other : null;
    }

    internal static string GetCategory(
        long n,
        string? culture = null,
        IReadOnlyDictionary<string, Func<long, string>>? customRules = null)
    {
        // 1. Consumer-defined rule — highest priority
        if (culture is not null && customRules is { Count: > 0 }
                                && customRules.TryGetValue(culture, out var customRule))
            return customRule(n);

        // 2. Built-in CLDR table
        if (culture is not null && BuiltInRules.TryGetValue(culture, out var builtIn))
            return builtIn(n);

        // 3. Default: Slavic rules (backward-compatible)
        return GetCategorySlavic(n);
    }

    private static string GetCategoryGermanic(long n)
    {
        if (n == 0) return "zero";
        return Math.Abs(n) == 1 ? "one" : "other";
    }

    private static string GetCategorySlavic(long n)
    {
        var abs = Math.Abs(n);
        var mod10 = abs % 10;
        var mod100 = abs % 100;

        if (n == 0) return "zero";
        if (mod10 == 1 && mod100 != 11) return "one";
        if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) return "few";
        return "many";
    }

    private static string GetCategoryFrench(long n)
    {
        if (n == 0) return "zero";
        return Math.Abs(n) is 0 or 1 ? "one" : "other";
    }

    private static string GetCategoryArabic(long n)
    {
        var abs = Math.Abs(n);
        var mod100 = abs % 100;

        if (n == 0) return "zero";
        if (abs == 1) return "one";
        if (abs == 2) return "two";
        if (mod100 >= 3 && mod100 <= 10) return "few";
        if (mod100 >= 11 && mod100 <= 99) return "many";
        return "other";
    }
}
