using NSubstitute;

namespace CoreLibs.Cache.Tests;

public class CompositeCacheTests
{
    private readonly ICache _primary = Substitute.For<ICache>();
    private readonly ICache _secondary = Substitute.For<ICache>();

    [Fact]
    public async Task GetAsync_ReturnsPrimaryValue_WhenPrimaryHasData()
    {
        var cache = new CompositeCache(_primary, _secondary);
        _primary.GetAsync<string>("key").Returns("from-primary");

        var result = await cache.GetAsync<string>("key");

        Assert.Equal("from-primary", result);
        await _secondary.DidNotReceive().GetAsync<string>("key");
    }

    [Fact]
    public async Task GetAsync_FallsToSecondary_WhenPrimaryMisses()
    {
        var cache = new CompositeCache(_primary, _secondary);
        _primary.GetAsync<string>("key").Returns((string?)null);
        _secondary.GetAsync<string>("key").Returns("from-secondary");

        var result = await cache.GetAsync<string>("key");

        Assert.Equal("from-secondary", result);
    }

    [Fact]
    public async Task GetAsync_BackfillsPrimary_OnSecondaryHit()
    {
        var primaryExpiry = TimeSpan.FromMinutes(5);
        var cache = new CompositeCache(_primary, _secondary, primaryExpiry);
        _primary.GetAsync<string>("key").Returns((string?)null);
        _secondary.GetAsync<string>("key").Returns("from-secondary");

        await cache.GetAsync<string>("key");

        await _primary.Received(1).SetAsync("key", "from-secondary", primaryExpiry, When.Always);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenBothMiss()
    {
        var cache = new CompositeCache(_primary, _secondary);
        _primary.GetAsync<string>("key").Returns((string?)null);
        _secondary.GetAsync<string>("key").Returns((string?)null);

        var result = await cache.GetAsync<string>("key");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_WritesToBothLayers()
    {
        var cache = new CompositeCache(_primary, _secondary);
        var expiry = TimeSpan.FromMinutes(10);
        _primary.SetAsync("key", "value", expiry, When.Always).Returns(true);
        _secondary.SetAsync("key", "value", expiry, When.Always).Returns(true);

        var result = await cache.SetAsync("key", "value", expiry);

        Assert.True(result);
        await _primary.Received(1).SetAsync("key", "value", expiry, When.Always);
        await _secondary.Received(1).SetAsync("key", "value", expiry, When.Always);
    }

    [Fact]
    public async Task DeleteAsync_DeletesFromBothLayers()
    {
        var cache = new CompositeCache(_primary, _secondary);
        _primary.DeleteAsync("key").Returns(true);
        _secondary.DeleteAsync("key").Returns(false);

        var result = await cache.DeleteAsync("key");

        Assert.True(result);
        await _primary.Received(1).DeleteAsync("key");
        await _secondary.Received(1).DeleteAsync("key");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenBothReturnFalse()
    {
        var cache = new CompositeCache(_primary, _secondary);
        _primary.DeleteAsync("key").Returns(false);
        _secondary.DeleteAsync("key").Returns(false);

        var result = await cache.DeleteAsync("key");

        Assert.False(result);
    }
}
