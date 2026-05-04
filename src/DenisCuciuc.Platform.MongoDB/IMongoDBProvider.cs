using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB;

public interface IMongoDBProvider
{
    IMongoClient Client { get; }
    IMongoDatabase Database { get; }
}
