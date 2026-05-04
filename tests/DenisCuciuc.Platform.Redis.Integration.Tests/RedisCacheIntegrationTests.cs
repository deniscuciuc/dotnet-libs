using DenisCuciuc.Platform.Cache;
using DenisCuciuc.Platform.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DenisCuciuc.Platform.Redis.Integration.Tests;

[Collection("Redis")]
public class RedisCacheIntegrationTests
{
    private readonly ICache _cache;

    public RedisCacheIntegrationTests(RedisFixture fixture)
    {
        var options = Options.Create(new RedisOptions { ConnectionString = fixture.ConnectionString });
        var client = new RedisClient(options, NullLogger<RedisClient>.Instance);
        client.ConnectAsync().GetAwaiter().GetResult();

        var serializer = new JsonCacheSerializer();
        _cache = new RedisCache(client, serializer);
    }

    // ── Get / Set ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenKeyNotFound()
    {
        var result = await _cache.GetAsync<string>($"missing-{Guid.NewGuid():N}");
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_And_GetAsync_RoundTrip_String()
    {
        var key = $"str-{Guid.NewGuid():N}";
        await _cache.SetAsync(key, "hello redis");

        var result = await _cache.GetAsync<string>(key);

        Assert.Equal("hello redis", result);
    }

    [Fact]
    public async Task SetAsync_And_GetAsync_RoundTrip_Object()
    {
        var key = $"obj-{Guid.NewGuid():N}";
        var value = new { Name = "Redis", Version = 7 };
        await _cache.SetAsync(key, value);

        var result = await _cache.GetAsync<dynamic>(key);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_Returns_True_OnSuccess()
    {
        var key = $"set-{Guid.NewGuid():N}";
        var result = await _cache.SetAsync(key, 42);

        Assert.True(result);
    }

    // ── Delete ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Returns_True_WhenKeyExists()
    {
        var key = $"del-{Guid.NewGuid():N}";
        await _cache.SetAsync(key, "delete me");

        var result = await _cache.DeleteAsync(key);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_WhenKeyMissing()
    {
        var key = $"del-missing-{Guid.NewGuid():N}";

        var result = await _cache.DeleteAsync(key);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_KeyNoLongerExists_AfterDelete()
    {
        var key = $"del-gone-{Guid.NewGuid():N}";
        await _cache.SetAsync(key, "value");
        await _cache.DeleteAsync(key);

        var result = await _cache.GetAsync<string>(key);

        Assert.Null(result);
    }

    // ── When.NotExists ────────────────────────────────────────────────────

    [Fact]
    public async Task SetAsync_WhenNotExists_DoesNotOverwrite_ExistingKey()
    {
        var key = $"nx-{Guid.NewGuid():N}";
        await _cache.SetAsync(key, "original");

        var result = await _cache.SetAsync(key, "overwrite", when: When.NotExists);
        var retrieved = await _cache.GetAsync<string>(key);

        Assert.False(result);
        Assert.Equal("original", retrieved);
    }

    [Fact]
    public async Task SetAsync_WhenNotExists_Sets_WhenKeyMissing()
    {
        var key = $"nx-new-{Guid.NewGuid():N}";

        var result = await _cache.SetAsync(key, "new value", when: When.NotExists);
        var retrieved = await _cache.GetAsync<string>(key);

        Assert.True(result);
        Assert.Equal("new value", retrieved);
    }

    // ── When.Exists ───────────────────────────────────────────────────────

    [Fact]
    public async Task SetAsync_WhenExists_Updates_ExistingKey()
    {
        var key = $"xx-{Guid.NewGuid():N}";
        await _cache.SetAsync(key, "original");

        var result = await _cache.SetAsync(key, "updated", when: When.Exists);
        var retrieved = await _cache.GetAsync<string>(key);

        Assert.True(result);
        Assert.Equal("updated", retrieved);
    }

    [Fact]
    public async Task SetAsync_WhenExists_DoesNotSet_WhenKeyMissing()
    {
        var key = $"xx-missing-{Guid.NewGuid():N}";

        var result = await _cache.SetAsync(key, "value", when: When.Exists);
        var retrieved = await _cache.GetAsync<string>(key);

        Assert.False(result);
        Assert.Null(retrieved);
    }

    // ── Expiry ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetAsync_WithExpiry_KeyExpires()
    {
        var key = $"ttl-{Guid.NewGuid():N}";
        await _cache.SetAsync(key, "temporary", expiry: TimeSpan.FromSeconds(1));

        await Task.Delay(TimeSpan.FromSeconds(2));

        var result = await _cache.GetAsync<string>(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_WithExpiry_KeyExistsBeforeExpiry()
    {
        var key = $"ttl-before-{Guid.NewGuid():N}";
        await _cache.SetAsync(key, "alive", expiry: TimeSpan.FromSeconds(10));

        var result = await _cache.GetAsync<string>(key);
        Assert.Equal("alive", result);
    }

    // ── Multiple Values ───────────────────────────────────────────────────

    [Fact]
    public async Task SetAsync_DifferentKeys_StoreIndependently()
    {
        var key1 = $"k1-{Guid.NewGuid():N}";
        var key2 = $"k2-{Guid.NewGuid():N}";
        await _cache.SetAsync(key1, "value1");
        await _cache.SetAsync(key2, "value2");

        var r1 = await _cache.GetAsync<string>(key1);
        var r2 = await _cache.GetAsync<string>(key2);

        Assert.Equal("value1", r1);
        Assert.Equal("value2", r2);
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        var key = $"overwrite-{Guid.NewGuid():N}";
        await _cache.SetAsync(key, "first");
        await _cache.SetAsync(key, "second");

        var result = await _cache.GetAsync<string>(key);
        Assert.Equal("second", result);
    }
}
