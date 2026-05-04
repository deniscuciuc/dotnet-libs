using MongoDB.Bson;

namespace DenisCuciuc.Platform.MongoDB.Actor;

/// <summary>
/// Short-lived entity cache for CAS retry loops.
/// </summary>
public interface IEntityCache<TEntity> where TEntity : EntityBase
{
    Task<TEntity?> GetAsync(ObjectId id);
    Task SetAsync(ObjectId id, TEntity entity, TimeSpan expiry);
    Task RemoveAsync(ObjectId id);
}
