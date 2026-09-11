using MongoDB.Driver;

namespace CoreLibs.MongoDB.Seeding;

public abstract class Seeder
{
    internal IMongoClient InternalClient = null!;
    internal IMongoDatabase InternalDatabase = null!;

    protected IMongoClient Client => InternalClient;
    protected IMongoDatabase Database => InternalDatabase;

    protected IMongoCollection<TDocument> GetCollection<TDocument>(string name)
    {
        return Database.GetCollection<TDocument>(name);
    }

    public abstract Task SeedAsync();

    public virtual bool ShouldSeed()
    {
        return true;
    }
}
