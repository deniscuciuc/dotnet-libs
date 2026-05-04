using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB.Seeding;

public class SeederRunner : ISeederRunner
{
    private const string CollectionName = "__seeders";

    private const string StatusRunning = "Running";
    private const string StatusSucceeded = "Succeeded";
    private const string StatusFailed = "Failed";

    private const string IndexNameStatus = "name_status_idx";
    private const string IndexName = "name_idx";
    private const string IndexUniqueRunningOrSucceeded = "uniq_running_or_succeeded";
    private static readonly TimeSpan StaleRunningThreshold = TimeSpan.FromMinutes(5);

    private readonly ILogger<SeederRunner>? _logger;
    private readonly IMongoDBProvider _provider;
    private readonly IReadOnlyList<SeederDescriptor> _seeders;

    public SeederRunner(
        IMongoDBProvider provider,
        IEnumerable<Seeder> seeders,
        ILogger<SeederRunner>? logger = null)
    {
        _provider = provider;
        _logger = logger;
        _seeders = seeders
            .Select(s =>
            {
                var attr = s.GetType().GetCustomAttributes(typeof(SeederAttribute), false)
                    .Cast<SeederAttribute>()
                    .SingleOrDefault();
                if (attr is null)
                    throw new InvalidOperationException(
                        $"Seeder '{s.GetType().FullName}' must be decorated with [Seeder(...)] attribute.");

                return new SeederDescriptor(attr.Name, s.GetType().Name, attr.Description, s);
            })
            .OrderBy(s => s.Name)
            .ToList();

        var duplicates = _seeders.GroupBy(s => s.Name).Where(g => g.Count() > 1).ToList();

        if (duplicates.Count <= 0) return;

        var dupText = string.Join(", ", duplicates.Select(d => d.Key));
        throw new InvalidOperationException($"Duplicate seeder names detected: {dupText}");
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var collection = _provider.Database.GetCollection<SeederEntry>(CollectionName);
        await EnsureIndexesAsync(collection, cancellationToken);

        foreach (var descriptor in _seeders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ExecuteSeederAsync(descriptor, collection, cancellationToken);
        }
    }

    private async Task ExecuteSeederAsync(
        SeederDescriptor descriptor,
        IMongoCollection<SeederEntry> collection,
        CancellationToken cancellationToken)
    {
        var seeder = descriptor.Instance;
        seeder.InternalClient = _provider.Client;
        seeder.InternalDatabase = _provider.Database;

        if (!seeder.ShouldSeed())
        {
            _logger?.LogInformation("Skipping seeder {Name} due to ShouldSeed=false", descriptor.Name);
            return;
        }

        var existsSucceeded = await collection.Find(x =>
                x.Name == descriptor.Name && x.Status == StatusSucceeded)
            .Project(x => x.Id)
            .Limit(1)
            .AnyAsync(cancellationToken);
        if (existsSucceeded)
        {
            _logger?.LogInformation(
                "Skipping seeder {Name} because it's already marked Succeeded.",
                descriptor.Name);
            return;
        }

        var runningRecord = await collection.Find(x =>
                x.Name == descriptor.Name && x.Status == StatusRunning)
            .Sort(Builders<SeederEntry>.Sort.Ascending(x => x.AppliedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
        if (runningRecord != null)
        {
            var age = DateTime.UtcNow - runningRecord.AppliedAtUtc;
            if (age > StaleRunningThreshold)
            {
                _logger?.LogWarning(
                    "Detected stale RUNNING seeder {Name} older than {AgeMinutes:F1} minutes. Marking as Failed and retrying.",
                    descriptor.Name, age.TotalMinutes);
                var update = Builders<SeederEntry>.Update
                    .Set(x => x.Status, StatusFailed)
                    .Set(x => x.Error, $"Recovered stale RUNNING (age {age}).")
                    .Set(x => x.DurationMs, (long)age.TotalMilliseconds);
                try
                {
                    await collection.UpdateOneAsync(x => x.Id == runningRecord.Id, update,
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    _logger?.LogError(
                        "Can't update seeder {RunningRecordId} with failed recovered RUNNING status.", runningRecord
                            .Id);
                }
            }
            else
            {
                _logger?.LogInformation(
                    "Another instance appears to be running seeder {Name} (age {AgeSeconds:F1}s). Skipping.",
                    descriptor.Name, age.TotalSeconds);
                return; // still within threshold -> assume active
            }
        }

        var sw = Stopwatch.StartNew();
        var insertedId = ObjectId.Empty;
        SeederEntry? entry = null;

        entry = new SeederEntry
        {
            Id = ObjectId.GenerateNewId(),
            Name = descriptor.Name,
            Description = descriptor.Description,
            AppliedAtUtc = DateTime.UtcNow,
            Status = StatusRunning,
            DurationMs = 0
        };

        try
        {
            // Insert RUNNING marker under partial unique index (Status in [Running, Succeeded])
            try
            {
                await collection.InsertOneAsync(entry, cancellationToken: cancellationToken);
                insertedId = entry.Id;
            }
            catch (MongoWriteException mwx) when (mwx.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                _logger?.LogInformation(
                    "Concurrent attempt detected for seeder {Name}. Another instance inserted RUNNING/SUCCEEDED. Skipping.",
                    descriptor.Name);
                return;
            }

            await seeder.SeedAsync();

            sw.Stop();
            var duration = sw.ElapsedMilliseconds;

            var update = Builders<SeederEntry>.Update
                .Set(x => x.Status, StatusSucceeded)
                .Set(x => x.DurationMs, duration)
                .Set(x => x.Error, null)
                .Set(x => x.AppliedAtUtc, entry.AppliedAtUtc)
                .Set(x => x.Description, entry.Description);
            await collection.UpdateOneAsync(x => x.Id == insertedId, update, cancellationToken: cancellationToken);

            _logger?.LogInformation("MongoDB seeder {Name} succeeded in {Ms} ms", descriptor.Name, duration);
        }
        catch (Exception ex)
        {
            sw.Stop();
            var duration = sw.ElapsedMilliseconds;

            if (insertedId != ObjectId.Empty)
            {
                var updateFail = Builders<SeederEntry>.Update
                    .Set(x => x.Status, StatusFailed)
                    .Set(x => x.Error, ex.ToString())
                    .Set(x => x.DurationMs, duration);
                try
                {
                    await collection.UpdateOneAsync(x => x.Id == insertedId, updateFail,
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    _logger?.LogError("Can't update seeder {InsertedId} with failed status.", insertedId);
                }
            }

            _logger?.LogError(ex, "MongoDB seeder {Name} failed after {Ms} ms", descriptor.Name, duration);
            throw;
        }
    }

    private static async Task EnsureIndexesAsync(IMongoCollection<SeederEntry> collection, CancellationToken ct)
    {
        // Drop duplicates of Succeeded by keeping earliest
        var succeeded = await collection
            .Find(Builders<SeederEntry>.Filter.Where(e => e.Status == StatusSucceeded))
            .Project(x => new { x.Id, x.Name, x.AppliedAtUtc })
            .ToListAsync(ct);
        var duplicates = succeeded.GroupBy(x => x.Name).Where(g => g.Count() > 1).ToList();
        if (duplicates.Count > 0)
        {
            var toRemove = duplicates.SelectMany(g => g.OrderBy(x => x.AppliedAtUtc).Skip(1).Select(x => x.Id))
                .ToList();
            if (toRemove.Count > 0)
                await collection.DeleteManyAsync(Builders<SeederEntry>.Filter.In(e => e.Id, toRemove), ct);
        }

        // Indexes
        var indexKeys = Builders<SeederEntry>.IndexKeys
            .Ascending(e => e.Name)
            .Ascending(e => e.Status);
        var indexModel = new CreateIndexModel<SeederEntry>(indexKeys,
            new CreateIndexOptions { Name = IndexNameStatus });

        var singleNameIndex = new CreateIndexModel<SeederEntry>(
            Builders<SeederEntry>.IndexKeys.Ascending(e => e.Name),
            new CreateIndexOptions { Name = IndexName, Unique = false });

        var uniqueRunningOrSucceeded = new CreateIndexModel<SeederEntry>(
            Builders<SeederEntry>.IndexKeys.Ascending(e => e.Name),
            new CreateIndexOptions<SeederEntry>
            {
                Name = IndexUniqueRunningOrSucceeded,
                Unique = true,
                PartialFilterExpression =
                    Builders<SeederEntry>.Filter.In(e => e.Status, [StatusRunning, StatusSucceeded])
            });

        await collection.Indexes.CreateManyAsync([indexModel, singleNameIndex, uniqueRunningOrSucceeded], ct);
    }

    private record SeederDescriptor(string Name, string TypeName, string? Description, Seeder Instance);

    private class SeederEntry
    {
        [BsonId] public ObjectId Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime AppliedAtUtc { get; set; }
        public long DurationMs { get; set; }
        public string Status { get; set; } = string.Empty; // Running / Succeeded / Failed
        public string? Error { get; set; }
    }
}
