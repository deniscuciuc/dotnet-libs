using NSubstitute;

namespace CoreLibs.Cache.Tests;

public class CacheExtensionsTests
{
    private readonly ICache _cache = Substitute.For<ICache>();

    [Fact]
    public async Task GetOrSetAsync_ReturnsCachedValue_OnHit()
    {
        _cache.GetAsync<string>("key").Returns("cached");
        var factoryCalled = false;

        var result = await _cache.GetOrSetAsync("key", () =>
        {
            factoryCalled = true;
            return Task.FromResult("fresh");
        });

        Assert.Equal("cached", result);
        Assert.False(factoryCalled);
    }

    [Fact]
    public async Task GetOrSetAsync_InvokesFactory_OnMiss()
    {
        _cache.GetAsync<string>("key").Returns((string?)null);
        _cache.SetAsync("key", "fresh", (TimeSpan?)null, When.Always).Returns(true);

        var result = await _cache.GetOrSetAsync("key", () => Task.FromResult("fresh"));

        Assert.Equal("fresh", result);
        await _cache.Received(1).SetAsync("key", "fresh", (TimeSpan?)null, When.Always);
    }

    [Fact]
    public async Task GetOrSetAsync_WithExpiry_PassesExpiryToSet()
    {
        var expiry = TimeSpan.FromMinutes(5);
        _cache.GetAsync<string>("num").Returns((string?)null);
        _cache.SetAsync("num", "42", expiry, When.Always).Returns(true);

        var result = await _cache.GetOrSetAsync("num", () => Task.FromResult("42"), expiry);

        Assert.Equal("42", result);
    }
}
