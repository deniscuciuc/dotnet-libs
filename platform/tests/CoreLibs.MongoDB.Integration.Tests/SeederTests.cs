using CoreLibs.MongoDB.Seeding;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CoreLibs.MongoDB.Integration.Tests;

// --- Concrete seeder implementations used in tests ---

[Seeder("initial-items", "Seeds initial test items")]
internal class InitialItemsSeeder : Seeder
{
    public bool Executed { get; private set; }

    public override async Task SeedAsync()
    {
        Executed = true;
        await GetCollection<BsonDocument>("seeded_items")
            .InsertOneAsync(new BsonDocument { ["name"] = "seeded-item" });
    }
}

[Seeder("extra-items", "Seeds extra items")]
internal class ExtraItemsSeeder : Seeder
{
    public bool Executed { get; private set; }

    public override async Task SeedAsync()
    {
        Executed = true;
        await GetCollection<BsonDocument>("seeded_items")
            .InsertManyAsync([
                new BsonDocument { ["name"] = "extra-1" },
                new BsonDocument { ["name"] = "extra-2" }
            ]);
    }
}

[Seeder("skip-seeder", "Always skipped via ShouldSeed=false")]
internal class AlwaysSkippedSeeder : Seeder
{
    public bool Executed { get; private set; }

    public override bool ShouldSeed() => false;

    public override Task SeedAsync()
    {
        Executed = true;
        return Task.CompletedTask;
    }
}

internal class SeederWithNoAttribute : Seeder
{
    public override Task SeedAsync() => Task.CompletedTask;
}

// --- Tests ---

[Collection("MongoDB")]
public class SeederTests(MongoDbFixture fixture)
{
    private IMongoDBProvider CreateProvider() =>
        fixture.CreateProvider($"seed_{Guid.NewGuid():N}");

    private static SeederRunner CreateRunner(IMongoDBProvider provider, params Seeder[] seeders) =>
        new(provider, seeders, NullLogger<SeederRunner>.Instance);

    [Fact]
    public async Task RunAsync_ExecutesSeederAndInsertsData()
    {
        var provider = CreateProvider();
        var seeder = new InitialItemsSeeder();
        var runner = CreateRunner(provider, seeder);

        await runner.RunAsync(CancellationToken.None);

        var count = await provider.Database
            .GetCollection<BsonDocument>("seeded_items")
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

        Assert.Equal(1, count);
        Assert.True(seeder.Executed);
    }

    [Fact]
    public async Task RunAsync_ExecutesMultipleSeedersInOrder()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new InitialItemsSeeder(), new ExtraItemsSeeder());

        await runner.RunAsync(CancellationToken.None);

        var count = await provider.Database
            .GetCollection<BsonDocument>("seeded_items")
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

        Assert.Equal(3, count); // 1 from initial + 2 from extra
    }

    [Fact]
    public async Task RunAsync_CreatesInternalSeedersCollection()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new InitialItemsSeeder());

        await runner.RunAsync(CancellationToken.None);

        var collections = (await provider.Database.ListCollectionNamesAsync()).ToList();
        Assert.Contains("__seeders", collections);
    }

    [Fact]
    public async Task RunAsync_Idempotent_DoesNotReseedSucceededSeeder()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new InitialItemsSeeder());

        await runner.RunAsync(CancellationToken.None);
        await runner.RunAsync(CancellationToken.None);

        var count = await provider.Database
            .GetCollection<BsonDocument>("seeded_items")
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RunAsync_ShouldSeedFalse_SkipsSeeder()
    {
        var provider = CreateProvider();
        var seeder = new AlwaysSkippedSeeder();
        var runner = CreateRunner(provider, seeder);

        await runner.RunAsync(CancellationToken.None);

        Assert.False(seeder.Executed);
    }

    [Fact]
    public async Task RunAsync_ShouldSeedFalse_DoesNotCreateTrackingEntry()
    {
        var provider = CreateProvider();
        var runner = CreateRunner(provider, new AlwaysSkippedSeeder());

        await runner.RunAsync(CancellationToken.None);

        // __seeders collection either doesn't exist or has no entries for the skipped seeder
        var collections = (await provider.Database.ListCollectionNamesAsync()).ToList();
        if (collections.Contains("__seeders"))
        {
            var count = await provider.Database
                .GetCollection<BsonDocument>("__seeders")
                .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
            Assert.Equal(0, count);
        }
    }

    [Fact]
    public void Constructor_ThrowsWhenSeederMissingAttribute()
    {
        var provider = CreateProvider();

        Assert.Throws<InvalidOperationException>(() =>
            CreateRunner(provider, new SeederWithNoAttribute()));
    }

    [Fact]
    public void Constructor_ThrowsOnDuplicateSeederNames()
    {
        var provider = CreateProvider();

        Assert.Throws<InvalidOperationException>(() =>
            CreateRunner(provider, new InitialItemsSeeder(), new InitialItemsSeeder()));
    }

    [Fact]
    public void SeederAttribute_ThrowsForBlankName()
    {
        Assert.Throws<ArgumentException>(() => new SeederAttribute(""));
        Assert.Throws<ArgumentException>(() => new SeederAttribute("   "));
    }

    [Fact]
    public void SeederAttribute_StoresNameAndDescription()
    {
        var attr = new SeederAttribute("my-seeder", "Does stuff");
        Assert.Equal("my-seeder", attr.Name);
        Assert.Equal("Does stuff", attr.Description);
    }
}
