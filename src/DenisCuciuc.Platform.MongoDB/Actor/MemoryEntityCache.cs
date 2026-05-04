using System.Collections.Concurrent;
using MongoDB.Bson;

namespace DenisCuciuc.Platform.MongoDB.Actor;

/// <summary>
/// Default in-memory entity cache using <see cref="ConcurrentDictionary{TKey,TValue}"/> with TTL.
/// Suitable for single-instance CAS retry windows (typically seconds).
/// Replace via <c>ActorRepositoryBuilder.WithCache&lt;T&gt;()</c> for distributed scenarios.
/// </summary>
public sealed class MemoryEntityCache<TEntity> : IEntityCache<TEntity>
    where TEntity : EntityBase
{
    private readonly ConcurrentDictionary<ObjectId, CacheEntry> _cache = new();

    public Task<TEntity?> GetAsync(ObjectId id)
    {
        if (_cache.TryGetValue(id, out var entry))
        {
            if (Environment.TickCount64 < entry.ExpiresAt)
                return Task.FromResult<TEntity?>(entry.Entity);

            _cache.TryRemove(id, out _);
        }

        return Task.FromResult<TEntity?>(null);
    }

    public Task SetAsync(ObjectId id, TEntity entity, TimeSpan expiry)
    {
        var expiresAt = Environment.TickCount64 + (long)expiry.TotalMilliseconds;
        _cache[id] = new CacheEntry(entity, expiresAt);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(ObjectId id)
    {
        _cache.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    private readonly record struct CacheEntry(TEntity Entity, long ExpiresAt);
}
