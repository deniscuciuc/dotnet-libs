using System.Linq.Expressions;
using CoreLibs.MongoDB.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CoreLibs.MongoDB;

/// <summary>
/// Repository for entities with ObjectId-based identity, typed filter/update builders,
/// atomic counters, and duplicate-key-safe operations.
/// Extends <see cref="RepositoryWithIndex{TDocument}"/> so indexes are auto-managed.
/// </summary>
public abstract class EntityRepository<TEntity> : RepositoryWithIndex<TEntity>, IRepository<TEntity>
    where TEntity : EntityBase
{
    private readonly IMongoDatabase _database;

    protected EntityRepository(
        IMongoDBProvider provider,
        IndexesBuilder<TEntity> indexes,
        ILogger logger)
        : base(GetCollectionNameForType(), provider, indexes)
    {
        _database = provider.Database;
        Logger = logger;
    }

    protected EntityRepository(
        string collectionName,
        IMongoDBProvider provider,
        IndexesBuilder<TEntity> indexes,
        ILogger logger)
        : base(collectionName, provider, indexes)
    {
        _database = provider.Database;
        Logger = logger;
    }

    // â”€â”€ Builder shortcuts â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public FilterDefinitionBuilder<TEntity> Filter => Builders<TEntity>.Filter;
    public UpdateDefinitionBuilder<TEntity> Update => Builders<TEntity>.Update;
    public ProjectionDefinitionBuilder<TEntity> Projection => Builders<TEntity>.Projection;
    public SortDefinitionBuilder<TEntity> Sort => Builders<TEntity>.Sort;
    public IndexKeysDefinitionBuilder<TEntity> Index => Builders<TEntity>.IndexKeys;

    protected internal ILogger Logger { get; }

    // â”€â”€ IRepository<TEntity> â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public IFindFluent<TEntity, TEntity> Find(FilterDefinition<TEntity> filter)
    {
        return Secondary.Find(filter);
    }

    public IFindFluent<BsonDocument, BsonDocument> Find(FilterDefinition<BsonDocument> filter)
    {
        return _database.GetSecondaryCollection<BsonDocument>(GetCollectionNameForType()).Find(filter);
    }

    public Task<long> IncrementAsync(string? suffix = null)
    {
        return _database.IncrementAsync($"{typeof(TEntity).Name}{suffix}");
    }

    // â”€â”€ Utilities â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    protected BsonDocument RenderFilter(FilterDefinition<TEntity> filter)
    {
        return filter.Render(new RenderArgs<TEntity>(Primary.DocumentSerializer, Primary.Settings.SerializerRegistry));
    }

    protected BsonDocument RenderFilter(Expression<Func<TEntity, bool>> filter)
    {
        return Builders<TEntity>.Filter.Where(filter)
            .Render(new RenderArgs<TEntity>(Primary.DocumentSerializer, Primary.Settings.SerializerRegistry));
    }

    protected async Task<bool> ExecuteAndCheckDuplicateAsync(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (MongoCommandException ex) when (ex.Code == 11000)
        {
            return false;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    protected async Task<TDocument?> ExecuteAndCheckDuplicateAsync<TDocument>(Func<Task<TDocument>> action)
    {
        try
        {
            return await action();
        }
        catch (MongoCommandException ex) when (ex.Code == 11000)
        {
            return default;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return default;
        }
    }

    private static string GetCollectionNameForType()
    {
        return typeof(TEntity).Name;
    }
}
