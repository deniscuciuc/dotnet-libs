namespace CoreLibs.LiveConfig.UnitTests;

public class ConfigCacheTests
{
    [Fact]
    public void Current_DefaultsToNull()
    {
        var cache = new ConfigCache<IReadOnlyList<string>>();

        Assert.Null(cache.Current);
        Assert.Equal(0, cache.CurrentVersion);
    }

    [Fact]
    public void Update_SetsCurrent()
    {
        var cache = new ConfigCache<IReadOnlyList<string>>();
        IReadOnlyList<string> data = ["a", "b"];

        cache.Update(data, 1);

        Assert.Same(data, cache.Current);
        Assert.Equal(1, cache.CurrentVersion);
    }

    [Fact]
    public void Invalidate_ClearsCurrent()
    {
        var cache = new ConfigCache<IReadOnlyList<string>>();
        IReadOnlyList<string> data = ["a"];

        cache.Update(data, 1);
        cache.Invalidate();

        Assert.Null(cache.Current);
        Assert.Equal(0, cache.CurrentVersion);
    }

    [Fact]
    public void Update_MultipleVersions_ReturnsLatest()
    {
        var cache = new ConfigCache<IReadOnlyList<int>>();

        cache.Update([1], 1);
        cache.Update([2], 2);
        cache.Update([3], 3);

        Assert.Equal([3], cache.Current);
        Assert.Equal(3, cache.CurrentVersion);
    }
}
