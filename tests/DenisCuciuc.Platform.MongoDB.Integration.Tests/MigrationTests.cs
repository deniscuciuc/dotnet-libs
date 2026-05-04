using DenisCuciuc.Platform.MongoDB.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB.Integration.Tests;

// --- Concrete migration implementations used in tests ---

[Migration(1, "Create items collection with seed document")]
internal class CreateItemsMigration : Migration
{
    public override async Task UpAsync()
    {
        await Database.CreateCollectionAsync("mig_items");
        await GetCollection<BsonDocument>("mig_items")
            .InsertOneAsync(new BsonDocument { ["v"] = 1 });
    }

    public override async Task DownAsync()
    {
        await Database.DropCollectionAsync("mig_items");
    }
}

[Migration(2, "Insert a second document into items collection")]
internal class AddSecondDocumentMigration : Migration
{
    public override async Task UpAsync()
    {
        await GetCollection<BsonDocument>("mig_items")
            .InsertOneAsync(new BsonDocument { ["v"] = 2 });
    }

    public override async Task DownAsync()
    {
        await GetCollection<BsonDocument>("mig_items")
            .DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("v", 2));
    }
}

[Migration(3, "Conditional migration — always skipped")]
internal class ConditionallySkippedMigration : Migration
{
    public override bool ShouldUp() => false;
    public override bool ShouldDown() => false;
    public override Task UpAsync() => Task.CompletedTask;
    public override Task DownAsync() => Task.CompletedTask;
}

internal class MigrationWithNoAttribute : Migration
{
    public override Task UpAsync() => Task.CompletedTask;
    public override Task DownAsync() => Task.CompletedTask;
}

// --- Tests ---

[Collection("MongoDB")]
public class MigrationTests(MongoDbFixture fixture)
{
    private IMongoDBProvider CreateProvider() =>
        fixture.CreateProvider($"mig_{Guid.NewGuid():N}");

    private static MigrationRunner CreateRunner(IMongoDBProvider provider, params Migration[] migrations) =>
        new(provider, migrations, NullLogger<MigrationRunner>.Instance);

    [Fact]
    public async Task RunAsync_AppliesAllMigrationsUpToLatest()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new CreateItemsMigration(), new AddSecondDocumentMigration());

        await runner.RunAsync(MigrationVersion.Latest, CancellationToken.None);

        var count = await provider.Database
            .GetCollection<BsonDocument>("mig_items")
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task RunAsync_CreatesInternalMigrationsCollection()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new CreateItemsMigration());

        await runner.RunAsync(MigrationVersion.Latest, CancellationToken.None);

        var collections = (await provider.Database.ListCollectionNamesAsync()).ToList();
        Assert.Contains("__migrations", collections);
    }

    [Fact]
    public async Task RunAsync_Idempotent_DoesNotReapplySucceededMigration()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new CreateItemsMigration());

        await runner.RunAsync(MigrationVersion.Latest, CancellationToken.None);
        await runner.RunAsync(MigrationVersion.Latest, CancellationToken.None);

        var count = await provider.Database
            .GetCollection<BsonDocument>("mig_items")
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RunAsync_PartialUpgrade_OnlyAppliesMigrationsUpToSpecifiedVersion()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new CreateItemsMigration(), new AddSecondDocumentMigration());

        await runner.RunAsync(new MigrationVersion(1), CancellationToken.None);

        var count = await provider.Database
            .GetCollection<BsonDocument>("mig_items")
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RunAsync_IncrementalUpgrade_AppliesOnlyNewMigrations()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new CreateItemsMigration(), new AddSecondDocumentMigration());

        await runner.RunAsync(new MigrationVersion(1), CancellationToken.None);
        await runner.RunAsync(MigrationVersion.Latest, CancellationToken.None);

        var count = await provider.Database
            .GetCollection<BsonDocument>("mig_items")
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task RunAsync_RollsBackMigrationsToTargetVersion()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new CreateItemsMigration(), new AddSecondDocumentMigration());

        await runner.RunAsync(MigrationVersion.Latest, CancellationToken.None);
        await runner.RunAsync(new MigrationVersion(0), CancellationToken.None);

        var collections = (await provider.Database.ListCollectionNamesAsync()).ToList();
        Assert.DoesNotContain("mig_items", collections);
    }

    [Fact]
    public async Task RunAsync_PartialRollback_KeepsEarlierMigrations()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new CreateItemsMigration(), new AddSecondDocumentMigration());

        await runner.RunAsync(MigrationVersion.Latest, CancellationToken.None);
        await runner.RunAsync(new MigrationVersion(1), CancellationToken.None);

        // v2 rolled back (deleted doc with v=2), v1 still applied (mig_items exists with 1 doc)
        var count = await provider.Database
            .GetCollection<BsonDocument>("mig_items")
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RunAsync_ShouldUpFalse_SkipsMigration()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new CreateItemsMigration(), new ConditionallySkippedMigration());

        await runner.RunAsync(MigrationVersion.Latest, CancellationToken.None);

        var count = await provider.Database
            .GetCollection<BsonDocument>("mig_items")
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

        // v1 ran, v3 was skipped — collection has 1 document
        Assert.Equal(1, count);
    }

    [Fact]
    public void Constructor_ThrowsWhenMigrationMissingAttribute()
    {
        var provider = CreateProvider();

        Assert.Throws<InvalidOperationException>(() =>
            CreateRunner(provider, new MigrationWithNoAttribute()));
    }

    [Fact]
    public void Constructor_ThrowsOnDuplicateMigrationVersions()
    {
        var provider = CreateProvider();

        Assert.Throws<InvalidOperationException>(() =>
            CreateRunner(provider, new CreateItemsMigration(), new CreateItemsMigration()));
    }

    [Fact]
    public void MigrationAttribute_ThrowsForVersionZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MigrationAttribute(0));
    }

    [Fact]
    public void MigrationAttribute_ThrowsForNegativeVersion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MigrationAttribute(-1));
    }

    [Fact]
    public void MigrationAttribute_StoresVersionAndDescription()
    {
        var attr = new MigrationAttribute(42, "My migration");
        Assert.Equal(42, attr.Version);
        Assert.Equal("My migration", attr.Description);
    }
}
