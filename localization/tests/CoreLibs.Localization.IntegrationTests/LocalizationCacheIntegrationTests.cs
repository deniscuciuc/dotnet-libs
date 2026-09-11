using CoreLibs.Localization;
using CoreLibs.Localization.Runtime;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
namespace CoreLibs.Localization.IntegrationTests;
/// <summary>
/// Integration-level tests for the full Localizer + Cache pipeline
/// (without external dependencies).
/// </summary>
public sealed class LocalizationCacheIntegrationTests
{
    [Fact]
    public void FullPipeline_LoadAndResolve_Works()
    {
        // Arrange
        var cache = new LocalizationCache();
        var opts = Options.Create(new LocalizationOptions
        {
            DefaultCulture = "en",
            FallbackChain = ["en"],
            ReturnKeyOnMissing = true,
            LogMissingKeys = false
        });
        var localizer = new Localizer(cache, opts, NullLogger<Localizer>.Instance);
        cache.Update("ru", new Dictionary<string, LocalizationValue>
        {
            ["offers.welcome.title"] = new() { Value = "Добро пожаловать, {name}!" },
            ["game.items.count"] = new()
            {
                Value = "{count} предметов",
                Plurals = new()
                {
                    ["one"] = "1 предмет",
                    ["few"] = "{count} предмета",
                    ["many"] = "{count} предметов"
                }
            }
        });
        cache.Update("en", new Dictionary<string, LocalizationValue>
        {
            ["offers.welcome.title"] = new() { Value = "Welcome, {name}!" }
        });
        // Act & Assert — interpolation
        Assert.Equal("Добро пожаловать, Denis!", localizer.Get("offers.welcome.title", "ru", LocalizationArgs.With("name", "Denis")));
        Assert.Equal("Welcome, Denis!", localizer.Get("offers.welcome.title", "en", LocalizationArgs.With("name", "Denis")));
        // Fallback: "de" not loaded → falls back to "en"
        Assert.Equal("Welcome, Denis!", localizer.Get("offers.welcome.title", "de", LocalizationArgs.With("name", "Denis")));
        // Missing key → returns key itself
        Assert.Equal("unknown.key", localizer.Get("unknown.key", "ru"));
    }
    [Fact]
    public async Task ConcurrentUpdates_DoNotCorruptCache()
    {
        var cache = new LocalizationCache();
        var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            var culture = i % 2 == 0 ? "ru" : "en";
            cache.Update(culture, new Dictionary<string, LocalizationValue>
            {
                [$"key{i}"] = new() { Value = $"value{i}" }
            });
        }));
        await Task.WhenAll(tasks);
        // Should have both cultures loaded and no exceptions
        Assert.NotNull(cache.GetProvider("ru"));
        Assert.NotNull(cache.GetProvider("en"));
    }
}
