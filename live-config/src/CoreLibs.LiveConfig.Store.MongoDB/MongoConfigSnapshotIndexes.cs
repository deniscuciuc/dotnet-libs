using CoreLibs.MongoDB;
using MongoDB.Driver;

namespace CoreLibs.LiveConfig.Store.MongoDB;

/// <summary>
/// Defines MongoDB indexes for the config snapshot collection.
/// Indexes are applied automatically during <c>RunMongoDBAsync()</c>.
/// </summary>
public sealed class MongoConfigSnapshotIndexes : IndexesBuilder<MongoConfigSnapshotEntity>
{
    public MongoConfigSnapshotIndexes()
    {
        // Unique composite: one version per config type
        Index(Builders<MongoConfigSnapshotEntity>.IndexKeys
                .Ascending(x => x.ConfigType)
                .Ascending(x => x.Version))
            .Unique()
            .Name("ix_configtype_version");

        // Query index: find active version per config type
        Index(Builders<MongoConfigSnapshotEntity>.IndexKeys
                .Ascending(x => x.ConfigType)
                .Descending(x => x.IsActive))
            .Name("ix_configtype_active");
    }
}
