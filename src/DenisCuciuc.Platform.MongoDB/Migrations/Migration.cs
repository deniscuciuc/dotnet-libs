using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB.Migrations;

public abstract class Migration
{
    internal IMongoClient InternalClient = null!;
    internal IMongoDatabase InternalDatabase = null!;

    protected IMongoClient Client => InternalClient;
    protected IMongoDatabase Database => InternalDatabase;

    protected IMongoCollection<TDocument> GetCollection<TDocument>(string name)
    {
        return Database.GetCollection<TDocument>(name);
    }

    public abstract Task UpAsync();
    public abstract Task DownAsync();

    public virtual bool ShouldUp()
    {
        return true;
    }

    public virtual bool ShouldDown()
    {
        return true;
    }
}
