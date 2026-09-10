using CoreLibs.Cache;
using Redis_When = StackExchange.Redis.When;

namespace CoreLibs.Redis;

public sealed class RedisCache(IRedisClient client, ICacheSerializer serializer) : ICache
{
    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await client.Database.StringGetAsync(key);
        if (data.IsNull) return default;

        var bytes = (byte[]?)data;
        return bytes is null ? default : serializer.Deserialize<T>(bytes);
    }

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always)
    {
        var bytes = serializer.Serialize(value);
        return client.Database.StringSetAsync(key, bytes, expiry, (Redis_When)(int)when);
    }

    public Task<bool> DeleteAsync(string key) =>
        client.Database.KeyDeleteAsync(key);
}
