using CoreLibs.Localization;
using CoreLibs.Localization.Runtime;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CoreLibs.Localization.UnitTests;

/// <summary>
/// Covers <see cref="Localizer.GetPlural"/>, which walks the same fallback chain as
/// <see cref="Localizer.Get"/> but resolves a CLDR plural form and injects <c>{count}</c>.
/// </summary>
public sealed class PluralResolutionTests
{
    private static Localizer BuildLocalizer(
        ILocalizationCache cache,
        Action<LocalizationOptions>? configure = null)
    {
        var options = new LocalizationOptions();
        configure?.Invoke(options);
        return new Localizer(cache, Options.Create(options), NullLogger<Localizer>.Instance);
    }

    private static LocalizationCache CacheWith(
        string culture,
        string key,
        LocalizationValue value)
    {
        var cache = new LocalizationCache();
        cache.Update(culture, new Dictionary<string, LocalizationValue> { [key] = value });
        return cache;
    }

    [Theory]
    [InlineData(1, "1 apple")]
    [InlineData(2, "2 apples")]
    [InlineData(0, "0 apples")]
    public void GetPlural_SelectsEnglishForm_ByCount(long count, string expected)
    {
        var cache = CacheWith("en", "apples", new LocalizationValue
        {
            Value = "{count} apples",
            Plurals = new Dictionary<string, string>
            {
                ["one"] = "{count} apple",
                ["other"] = "{count} apples"
            }
        });

        var localizer = BuildLocalizer(cache);

        Assert.Equal(expected, localizer.GetPlural("apples", "en", count));
    }

    [Fact]
    public void GetPlural_InjectsCount_IntoTemplate()
    {
        var cache = CacheWith("en", "items", new LocalizationValue { Value = "You have {count} items" });

        var localizer = BuildLocalizer(cache);

        Assert.Equal("You have 7 items", localizer.GetPlural("items", "en", 7));
    }

    [Fact]
    public void GetPlural_PreservesCallerArgs_AlongsideCount()
    {
        var cache = CacheWith("en", "cart", new LocalizationValue { Value = "{name} has {count} items" });

        var localizer = BuildLocalizer(cache);

        var result = localizer.GetPlural("cart", "en", 3, LocalizationArgs.With("name", "Ana"));

        Assert.Equal("Ana has 3 items", result);
    }

    [Fact]
    public void GetPlural_FallsBackToChain_WhenCultureMissing()
    {
        var cache = CacheWith("en", "files", new LocalizationValue
        {
            Value = "{count} files",
            Plurals = new Dictionary<string, string> { ["one"] = "{count} file", ["other"] = "{count} files" }
        });

        var localizer = BuildLocalizer(cache, o => o.FallbackChain = ["en"]);

        Assert.Equal("1 file", localizer.GetPlural("files", "ro", 1));
    }

    [Fact]
    public void GetPlural_UsesFlatValue_WhenNoPluralFormsDefined()
    {
        var cache = CacheWith("en", "notifications", new LocalizationValue { Value = "{count} new" });

        var localizer = BuildLocalizer(cache);

        Assert.Equal("5 new", localizer.GetPlural("notifications", "en", 5));
    }

    [Fact]
    public void GetPlural_ReturnsKey_WhenMissingEverywhere()
    {
        var localizer = BuildLocalizer(new LocalizationCache(), o => o.LogMissingKeys = false);

        Assert.Equal("nothing.here", localizer.GetPlural("nothing.here", "en", 1));
    }

    [Fact]
    public void GetPlural_ReturnsEmpty_WhenReturnKeyOnMissingIsDisabled()
    {
        var localizer = BuildLocalizer(new LocalizationCache(), o =>
        {
            o.LogMissingKeys = false;
            o.ReturnKeyOnMissing = false;
        });

        Assert.Equal(string.Empty, localizer.GetPlural("nothing.here", "en", 1));
    }

    [Fact]
    public void GetPlural_HonoursCustomPluralRule_OverBuiltInTable()
    {
        var cache = CacheWith("tr", "guests", new LocalizationValue
        {
            Value = "{count} misafir",
            // Turkish has a single form; a custom rule routes every count to "other".
            Plurals = new Dictionary<string, string> { ["one"] = "bir misafir", ["other"] = "{count} misafir" }
        });

        var localizer = BuildLocalizer(cache, o => o.PluralRules["tr"] = _ => "other");

        Assert.Equal("1 misafir", localizer.GetPlural("guests", "tr", 1));
    }
}
