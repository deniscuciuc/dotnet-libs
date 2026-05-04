using DenisCuciuc.Platform.Cache;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace DenisCuciuc.Platform.MongoDB.Cache;

/// <summary>
/// <see cref="ICache"/> implementation that serializes values using MongoDB's BSON/extended JSON
/// serializer before delegating storage to an inner <see cref="ICache"/> (e.g. Redis).
/// Useful when the same BSON type mappings registered for MongoDB documents should also govern
/// how objects are cached.
/// </summary>
public class MongoCache(ICache inner, string? prefix = null) : ICache
{
    public async Task<T?> GetAsync<T>(string key)
    {
        EnsureClassMap<T>();

        var json = await inner.GetAsync<string>(BuildKey<T>(key));
        return json switch
        {
            null or "null" => default,
            _ => await Task.Run<T?>(() => BsonSerializer.Deserialize<T>(json))
        };
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always)
    {
        EnsureClassMap<T>();

        var json = value is null
            ? "null"
            : await Task.Run(() => value.ToJson());

        return await inner.SetAsync(BuildKey<T>(key), json, expiry, when);
    }

    public Task<bool> DeleteAsync(string key) =>
        inner.DeleteAsync(key);

    private string BuildKey<T>(string key) =>
        $"{prefix ?? ""}{typeof(T).Name}/{key}";

    private static void EnsureClassMap<T>()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            BsonClassMap.RegisterClassMap<T>(cm => cm.AutoMap());
    }
}
