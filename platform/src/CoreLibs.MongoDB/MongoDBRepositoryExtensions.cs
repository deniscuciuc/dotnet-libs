using MongoDB.Driver;

namespace CoreLibs.MongoDB;

public static class MongoDBRepositoryExtensions
{
    public static FilterDefinitionBuilder<T> Filter<T>(this Repository<T> repository)
    {
        return Builders<T>.Filter;
    }

    public static IndexKeysDefinitionBuilder<T> Index<T>(this Repository<T> repository)
    {
        return Builders<T>.IndexKeys;
    }

    public static ProjectionDefinitionBuilder<T> Projection<T>(this Repository<T> repository)
    {
        return Builders<T>.Projection;
    }

    public static UpdateDefinitionBuilder<T> Update<T>(this Repository<T> repository)
    {
        return Builders<T>.Update;
    }

    public static SortDefinitionBuilder<T> Sort<T>(this Repository<T> repository)
    {
        return Builders<T>.Sort;
    }
}
