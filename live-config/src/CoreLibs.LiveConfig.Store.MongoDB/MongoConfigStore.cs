using CoreLibs.MongoDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CoreLibs.LiveConfig.Store.MongoDB;

/// <summary>
/// MongoDB implementation of <see cref="IConfigStore"/> using the CoreLibs.MongoDB
/// <see cref="RepositoryWithIndex{TDocument}"/> pattern. Indexes are applied
/// automatically during <c>RunMongoDBAsync()</c> at startup.
/// </summary>
public sealed class MongoConfigStore(
    IMongoDBProvider provider,
    MongoConfigSnapshotIndexes indexes,
    IOptions<MongoConfigStoreOptions> options,
    ILogger<MongoConfigStore> logger)
    : RepositoryWithIndex<MongoConfigSnapshotEntity>(
        options.Value.CollectionName, provider, indexes), IConfigStore
{
    public async Task<ConfigVersion?> GetActiveAsync(string configType, CancellationToken cancellationToken = default)
    {
        var entity = await Collection
            .Find(x => x.ConfigType == configType && x.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToConfigVersion(entity);
    }

    public async Task<ConfigVersion?> GetVersionAsync(string configType, int version,
        CancellationToken cancellationToken = default)
    {
        var entity = await Collection
            .Find(x => x.ConfigType == configType && x.Version == version)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToConfigVersion(entity);
    }

    public async Task<IReadOnlyList<ConfigVersion>> GetRecentVersionsAsync(string configType, int count = 10,
        CancellationToken cancellationToken = default)
    {
        var entities = await Collection
            .Find(x => x.ConfigType == configType)
            .SortByDescending(x => x.Version)
            .Limit(count)
            .ToListAsync(cancellationToken);

        return entities.Select(ToConfigVersion).ToList();
    }

    public async Task<int> GetLatestVersionNumberAsync(string configType, CancellationToken cancellationToken = default)
    {
        var entity = await Collection
            .Find(x => x.ConfigType == configType)
            .SortByDescending(x => x.Version)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);

        return entity?.Version ?? 0;
    }

    public async Task<string?> GetActiveHashAsync(string configType, CancellationToken cancellationToken = default)
    {
        var entity = await Collection
            .Find(x => x.ConfigType == configType && x.IsActive)
            .Project(x => new { x.DataHash })
            .FirstOrDefaultAsync(cancellationToken);

        return entity?.DataHash;
    }

    public async Task<ActiveVersionInfo?> GetActiveVersionInfoAsync(string configType,
        CancellationToken cancellationToken = default)
    {
        var entity = await Collection
            .Find(x => x.ConfigType == configType && x.IsActive)
            .Project(x => new { x.DataHash, x.Version })
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : new ActiveVersionInfo(entity.DataHash, entity.Version);
    }

    public async Task<int> CreateVersionAsync(string configType, string dataJson, string dataHash,
        string? createdBy = null, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var latestVersion = await GetLatestVersionNumberAsync(configType, cancellationToken);
            var newVersion = latestVersion + 1;

            var entity = new MongoConfigSnapshotEntity
            {
                ConfigType = configType,
                Version = newVersion,
                DataJson = dataJson,
                DataHash = dataHash,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            try
            {
                await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
                return newVersion;
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                logger.LogWarning(
                    "Duplicate key on create version {Version} for {ConfigType}, retrying (attempt {Attempt})",
                    newVersion, configType, attempt + 1);
            }
        }

        throw new InvalidOperationException($"Failed to create version for {configType} after {maxRetries} attempts");
    }

    public async Task<bool> ActivateVersionAsync(string configType, int version,
        CancellationToken cancellationToken = default)
    {
        // Atomically set isActive based on version match in a single UpdateMany.
        // All documents for this configType get isActive = (version == targetVersion).
        var filter = Builders<MongoConfigSnapshotEntity>.Filter.Eq(x => x.ConfigType, configType);
        var pipeline = new EmptyPipelineDefinition<MongoConfigSnapshotEntity>()
            .AppendStage<MongoConfigSnapshotEntity, MongoConfigSnapshotEntity, MongoConfigSnapshotEntity>(
                BsonDocument.Parse(
                    $"{{ $set: {{ isActive: {{ $eq: ['$version', {version}] }} }} }}"));

        var result = await Collection.UpdateManyAsync(filter, pipeline, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        var activeConfigs = await Collection
            .Find(x => x.IsActive)
            .Project(x => new { x.ConfigType, x.Version })
            .ToListAsync(cancellationToken);

        return activeConfigs.ToDictionary(x => x.ConfigType, x => x.Version);
    }

    public async Task<int> CleanupOldVersionsAsync(string configType, ConfigRetentionOptions retention,
        CancellationToken cancellationToken = default)
    {
        if (!retention.Enabled || retention.MaxVersions <= 0 && retention.MaxAge is null)
            return 0;

        var versions = await Collection
            .Find(x => x.ConfigType == configType)
            .SortByDescending(x => x.Version)
            .Project(x => new { x.Id, x.Version, x.IsActive, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var cutoffDate = retention.MaxAge is not null
            ? DateTime.UtcNow - retention.MaxAge.Value
            : (DateTime?)null;

        var idsToDelete = new List<ObjectId>();
        var index = 0;

        foreach (var v in versions)
        {
            index++;

            // Never delete the active version
            if (retention.KeepActiveAlways && v.IsActive)
                continue;

            var withinCountLimit = retention.MaxVersions > 0 && index <= retention.MaxVersions;
            var withinAgeLimit = cutoffDate is not null && v.CreatedAt >= cutoffDate;

            // Keep if within count limit OR within age limit
            if (withinCountLimit || withinAgeLimit)
                continue;

            // If only age-based (MaxVersions == 0), only delete if older than MaxAge
            if (retention.MaxVersions <= 0 && cutoffDate is null)
                continue;

            idsToDelete.Add(v.Id);
        }

        if (idsToDelete.Count == 0)
            return 0;

        var filter = Builders<MongoConfigSnapshotEntity>.Filter.In(x => x.Id, idsToDelete);
        var result = await Collection.DeleteManyAsync(filter, cancellationToken);
        var deleted = (int)result.DeletedCount;

        logger.LogInformation(
            "Retention cleanup for {ConfigType}: deleted {DeletedCount} old version(s), kept {KeptCount}",
            configType, deleted, versions.Count - deleted);

        return deleted;
    }

    private static ConfigVersion ToConfigVersion(MongoConfigSnapshotEntity entity)
    {
        return new ConfigVersion(
            entity.ConfigType,
            entity.Version,
            entity.DataJson,
            entity.DataHash,
            entity.IsActive,
            new DateTimeOffset(entity.CreatedAt, TimeSpan.Zero),
            entity.CreatedBy);
    }
}
