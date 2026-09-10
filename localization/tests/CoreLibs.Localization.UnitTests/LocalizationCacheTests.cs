using CoreLibs.Localization;
using CoreLibs.Localization.Runtime;
using NSubstitute;
namespace CoreLibs.Localization.UnitTests;

public sealed class LocalizationCacheTests
{
    [Fact]
    public void Update_SetsProvider_ForCulture()
    {
        var cache = new LocalizationCache();
        var entries = new Dictionary<string, LocalizationValue>
        {
            ["hello"] = new() { Value = "Привет" }
        };
        cache.Update("ru", entries);
        var provider = cache.GetProvider("ru");
        Assert.NotNull(provider);
        Assert.Equal("ru", provider.Culture);
    }
    [Fact]
    public void GetProvider_ReturnsNull_ForUnknownCulture()
    {
        var cache = new LocalizationCache();
        Assert.Null(cache.GetProvider("de"));
    }
    [Fact]
    public void Update_IsAtomic_MultipleCultures()
    {
        var cache = new LocalizationCache();
        cache.Update("ru", new Dictionary<string, LocalizationValue> { ["a"] = new() { Value = "А" } });
        cache.Update("en", new Dictionary<string, LocalizationValue> { ["a"] = new() { Value = "A" } });
        var cultures = cache.GetLoadedCultures();
        Assert.Contains("ru", cultures);
        Assert.Contains("en", cultures);
    }
    [Fact]
    public void Update_Overwrites_ExistingProvider()
    {
        var cache = new LocalizationCache();
        cache.Update("ru", new Dictionary<string, LocalizationValue> { ["hi"] = new() { Value = "v1" } });
        cache.Update("ru", new Dictionary<string, LocalizationValue> { ["hi"] = new() { Value = "v2" } });
        var provider = cache.GetProvider("ru");
        Assert.True(provider!.TryGet("hi", out var val));
        Assert.Equal("v2", val!.Value);
    }
}
