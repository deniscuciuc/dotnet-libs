using CoreLibs.Localization;
using CoreLibs.Localization.Runtime;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
namespace CoreLibs.Localization.UnitTests;

public sealed class LocalizerTests
{
    private static Localizer BuildLocalizer(
        ILocalizationCache cache,
        Action<LocalizationOptions>? configure = null)
    {
        var opts = new LocalizationOptions();
        configure?.Invoke(opts);
        return new Localizer(cache, Options.Create(opts), NullLogger<Localizer>.Instance);
    }
    [Fact]
    public void Get_ReturnsTranslation_ForKnownKey()
    {
        var cache = new LocalizationCache();
        cache.Update("ru", new Dictionary<string, LocalizationValue>
        {
            ["welcome"] = new() { Value = "Добро пожаловать" }
        });
        var localizer = BuildLocalizer(cache);
        var result = localizer.Get("welcome", "ru");
        Assert.Equal("Добро пожаловать", result);
    }
    [Fact]
    public void Get_FallsBack_ToDefaultCulture()
    {
        var cache = new LocalizationCache();
        cache.Update("en", new Dictionary<string, LocalizationValue>
        {
            ["greeting"] = new() { Value = "Hello" }
        });
        var localizer = BuildLocalizer(cache, o => o.FallbackChain = ["en"]);
        // Request "ru" but only "en" is loaded → falls back
        var result = localizer.Get("greeting", "ru");
        Assert.Equal("Hello", result);
    }
    [Fact]
    public void Get_ReturnsKey_WhenMissing_AndReturnKeyOnMissingTrue()
    {
        var cache = new LocalizationCache();
        var localizer = BuildLocalizer(cache, o => { o.ReturnKeyOnMissing = true; o.LogMissingKeys = false; });
        var result = localizer.Get("missing.key", "ru");
        Assert.Equal("missing.key", result);
    }
    [Fact]
    public void Get_WithArgs_InterpolatesPlaceholders()
    {
        var cache = new LocalizationCache();
        cache.Update("en", new Dictionary<string, LocalizationValue>
        {
            ["hello.user"] = new() { Value = "Hello {name}!" }
        });
        var localizer = BuildLocalizer(cache);
        var result = localizer.Get("hello.user", "en", LocalizationArgs.With("name", "Denis"));
        Assert.Equal("Hello Denis!", result);
    }
}
