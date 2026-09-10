using Microsoft.Extensions.Logging.Abstractions;

namespace CoreLibs.LiveConfig.UnitTests;

public class CacheConfigApplierTests
{
    [Fact]
    public async Task ApplyAsync_DeserializesJsonAndUpdatesCache()
    {
        var cache = new ConfigCache<IReadOnlyList<TestItem>>();
        var applier =
            new CacheConfigApplier<TestItem>(cache, "test-items", NullLogger<CacheConfigApplier<TestItem>>.Instance);

        var json = """[{"name":"A","value":1},{"name":"B","value":2}]""";
        await applier.ApplyAsync(json, 1);

        Assert.NotNull(cache.Current);
        Assert.Equal(2, cache.Current!.Count);
        Assert.Equal("A", cache.Current[0].Name);
        Assert.Equal(2, cache.Current[1].Value);
        Assert.Equal(1, cache.CurrentVersion);
    }

    [Fact]
    public void ConfigType_MatchesConstructorArgument()
    {
        var cache = new ConfigCache<IReadOnlyList<TestItem>>();
        var applier =
            new CacheConfigApplier<TestItem>(cache, "my-config", NullLogger<CacheConfigApplier<TestItem>>.Instance);

        Assert.Equal("my-config", applier.ConfigType);
    }

    private record TestItem(string Name, int Value);
}
