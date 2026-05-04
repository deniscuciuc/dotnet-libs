using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB.Integration.Tests;

[Collection("MongoDB")]
public class IndexTests(MongoDbFixture fixture)
{
    private TestItemRepository CreateRepository(string? prefix = null) =>
        new(fixture.CreateProvider($"{prefix ?? "idx"}_{Guid.NewGuid():N}"));

    [Fact]
    public async Task ApplyAsync_CreatesUniqueIndexOnName()
    {
        var repo = CreateRepository();
        await ((IRepositoryApplyIndex)repo).ApplyAsync(NullLogger.Instance);

        var indexList = await repo.Collection.Indexes.List().ToListAsync();

        Assert.True(indexList.Count >= 2);
        Assert.Contains(indexList, i => i["name"].AsString != "_id_");
    }

    [Fact]
    public async Task UniqueIndex_PreventsDuplicateValues()
    {
        var repo = CreateRepository();
        await ((IRepositoryApplyIndex)repo).ApplyAsync(NullLogger.Instance);

        await repo.InsertAsync(new TestItem { Name = "Unique" });

        await Assert.ThrowsAsync<MongoWriteException>(() =>
            repo.InsertAsync(new TestItem { Name = "Unique" }));
    }

    [Fact]
    public async Task ApplyAsync_IsIdempotent_DoesNotDoubleIndex()
    {
        var repo = CreateRepository();
        await ((IRepositoryApplyIndex)repo).ApplyAsync(NullLogger.Instance);
        await ((IRepositoryApplyIndex)repo).ApplyAsync(NullLogger.Instance);

        var indexList = await repo.Collection.Indexes.List().ToListAsync();

        // _id_ + 1 custom index — not duplicated
        Assert.Equal(2, indexList.Count);
    }

    [Fact]
    public async Task WithoutApplyAsync_NoDuplicateConstraintEnforced()
    {
        var repo = CreateRepository();

        // Two items with same Name succeed when index is not applied
        await repo.InsertAsync(new TestItem { Name = "Dup" });
        await repo.InsertAsync(new TestItem { Name = "Dup" });

        var all = await repo.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task ApplyAsync_DropsObsoleteIndexesNotInDefinition()
    {
        var provider = fixture.CreateProvider($"idx_drop_{Guid.NewGuid():N}");

        // Create a repo whose index builder defines one unique index
        var repo = new TestItemRepository(provider);
        await ((IRepositoryApplyIndex)repo).ApplyAsync(NullLogger.Instance);

        // Manually create an extra index that is NOT part of the definition
        var extraModel = new CreateIndexModel<TestItem>(
            Builders<TestItem>.IndexKeys.Ascending(x => x.Quantity),
            new CreateIndexOptions { Name = "extra_quantity_idx" });
        await repo.Collection.Indexes.CreateOneAsync(extraModel);

        var before = await repo.Collection.Indexes.List().ToListAsync();
        Assert.Equal(3, before.Count); // _id_ + name + extra_quantity

        // Re-apply — extra index should be dropped
        await ((IRepositoryApplyIndex)repo).ApplyAsync(NullLogger.Instance);

        var after = await repo.Collection.Indexes.List().ToListAsync();
        Assert.Equal(2, after.Count); // _id_ + name
        Assert.DoesNotContain(after, i => i["name"].AsString == "extra_quantity_idx");
    }

    [Fact]
    public async Task TwoRepositoriesOnSameProvider_ShareCollection()
    {
        var provider = fixture.CreateProvider($"shared_{Guid.NewGuid():N}");
        var repo1 = new TestItemRepository(provider);
        var repo2 = new TestItemRepository(provider);

        await repo1.InsertAsync(new TestItem { Name = "FromRepo1" });

        var all = await repo2.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("FromRepo1", all[0].Name);
    }
}
