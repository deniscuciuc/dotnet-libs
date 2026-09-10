using MongoDB.Bson;
using MongoDB.Driver;

namespace CoreLibs.MongoDB;

public interface IRepository;

public interface IRepository<TEntity> : IRepository
    where TEntity : EntityBase
{
    FilterDefinitionBuilder<TEntity> Filter { get; }

    IFindFluent<TEntity, TEntity> Find(FilterDefinition<TEntity> filter);

    IFindFluent<BsonDocument, BsonDocument> Find(FilterDefinition<BsonDocument> filter);

    Task<long> IncrementAsync(string? suffix = null);
}
