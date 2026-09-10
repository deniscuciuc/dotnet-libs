using MongoDB.Driver;

namespace CoreLibs.MongoDB.Extensions;

public static class MongoDatabaseExtensions
{
    public static IMongoCollection<TEntity> GetPrimaryCollection<TEntity>(
        this IMongoDatabase database,
        string collectionName)
    {
        return database.GetCollection<TEntity>(collectionName, new MongoCollectionSettings
        {
            ReadPreference = ReadPreference.Primary
        });
    }

    public static IMongoCollection<TEntity> GetSecondaryCollection<TEntity>(
        this IMongoDatabase database,
        string collectionName)
    {
        return database.GetCollection<TEntity>(collectionName, new MongoCollectionSettings
        {
            ReadPreference = ReadPreference.SecondaryPreferred
        });
    }

    public static async Task<long> IncrementAsync(
        this IMongoDatabase database,
        string counterName,
        string collectionName = "_counters")
    {
        var collection = database.GetCollection<Counter>(collectionName);

        var filter = Builders<Counter>.Filter.Eq(x => x.Id, counterName);

        var update = Builders<Counter>.Update
            .SetOnInsert(x => x.Id, counterName)
            .Inc(x => x.Value, 1);

        var projection = Builders<Counter>.Projection.Expression(x => x.Value);

        var options = new FindOneAndUpdateOptions<Counter, long>
        {
            IsUpsert = true,
            Projection = projection,
            ReturnDocument = ReturnDocument.After
        };

        return await collection.FindOneAndUpdateAsync(filter, update, options);
    }

    private class Counter
    {
        public string Id { get; set; } = null!;
        public long Value { get; set; }
    }
}
