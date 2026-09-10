using CoreLibs.Localization;
using CoreLibs.MongoDB;
using MongoDB.Driver;

namespace CoreLibs.Localization.Store.MongoDB;

/// <summary>
/// Registered as Singleton — MongoDB driver is thread-safe by design.
/// </summary>
public sealed class MongoLocalizationStore(IMongoDBProvider provider)
    : RepositoryWithIndex<MongoLocalizationEntity>("Localizations", provider, new Indexes()),
        ILocalizationStore
{
    public async Task<IEnumerable<LocalizationEntry>> FindAllAsync(
        string culture, CancellationToken ct = default)
    {
        var docs = await Secondary
            .Find(Builders<MongoLocalizationEntity>.Filter.Eq(x => x.Culture, culture))
            .ToListAsync(ct);

        return docs.Select(d => new LocalizationEntry
        {
            Key = d.Key,
            Culture = d.Culture,
            Value = d.Value
        });
    }

    public async Task UpsertAsync(LocalizationEntry entry, CancellationToken ct = default)
    {
        var doc = new MongoLocalizationEntity
        {
            Id = $"{entry.Culture}:{entry.Key}",
            Key = entry.Key,
            Culture = entry.Culture,
            Value = entry.Value
        };
        await Collection.ReplaceOneAsync(
            Builders<MongoLocalizationEntity>.Filter.Eq(x => x.Id, doc.Id),
            doc,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    public async Task UpsertManyAsync(IEnumerable<LocalizationEntry> entries, CancellationToken ct = default)
    {
        var models = entries.Select(entry =>
        {
            var doc = new MongoLocalizationEntity
            {
                Id = $"{entry.Culture}:{entry.Key}",
                Key = entry.Key,
                Culture = entry.Culture,
                Value = entry.Value
            };
            return new ReplaceOneModel<MongoLocalizationEntity>(
                    Builders<MongoLocalizationEntity>.Filter.Eq(x => x.Id, doc.Id), doc)
            { IsUpsert = true };
        }).ToList();

        if (models.Count > 0)
            await Collection.BulkWriteAsync(models, cancellationToken: ct);
    }

    public async Task DeleteAsync(string key, string culture, CancellationToken ct = default)
    {
        var id = $"{culture}:{key}";
        await Collection.DeleteOneAsync(
            Builders<MongoLocalizationEntity>.Filter.Eq(x => x.Id, id),
            ct);
    }

    public async Task<bool> ExistsAsync(string key, string culture, CancellationToken ct = default)
    {
        var id = $"{culture}:{key}";
        var count = await Secondary.CountDocumentsAsync(
            Builders<MongoLocalizationEntity>.Filter.Eq(x => x.Id, id),
            cancellationToken: ct);
        return count > 0;
    }

    public Task EnsureCreatedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    private sealed class Indexes : IndexesBuilder<MongoLocalizationEntity>
    {
        public Indexes()
        {
            Index(Ascending(x => x.Culture));
            Index(Ascending(x => x.Key));
        }
    }
}
