using MongoDB.Driver;

namespace CoreLibs.MongoDB;

public class MongoDBProvider(
    string databaseName,
    MongoClientSettings settings
) : IMongoDBProvider, IMongoDBConnection
{
    public Task ConnectAsync()
    {
        Client = new MongoClient(settings);
        Database = Client.GetDatabase(databaseName);
        return Task.CompletedTask;
    }

    public IMongoClient Client { get; private set; } = null!;
    public IMongoDatabase Database { get; private set; } = null!;
}
