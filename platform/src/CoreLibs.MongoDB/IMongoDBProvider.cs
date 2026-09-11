using MongoDB.Driver;

namespace CoreLibs.MongoDB;

public interface IMongoDBProvider
{
    IMongoClient Client { get; }
    IMongoDatabase Database { get; }
}
