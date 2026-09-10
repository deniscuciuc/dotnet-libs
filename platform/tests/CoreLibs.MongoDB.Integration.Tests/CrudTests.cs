using MongoDB.Bson;

namespace CoreLibs.MongoDB.Integration.Tests;

[Collection("MongoDB")]
public class CrudTests(MongoDbFixture fixture)
{
    private TestItemRepository CreateRepository() =>
        new(fixture.CreateProvider($"crud_{Guid.NewGuid():N}"));

    [Fact]
    public async Task Insert_And_GetById_ReturnsDocument()
    {
        var repo = CreateRepository();
        var item = new TestItem { Name = "Widget", Quantity = 10 };

        await repo.InsertAsync(item);
        var found = await repo.GetByIdAsync(item.Id);

        Assert.NotNull(found);
        Assert.Equal("Widget", found.Name);
        Assert.Equal(10, found.Quantity);
    }

    [Fact]
    public async Task Insert_SetsCreatedAtAndModifiedAt()
    {
        var repo = CreateRepository();
        var before = DateTime.UtcNow.AddSeconds(-1);
        var item = new TestItem { Name = "Timestamped" };

        await repo.InsertAsync(item);
        var found = await repo.GetByIdAsync(item.Id);

        Assert.NotNull(found);
        Assert.True(found.CreatedAt >= before);
        Assert.True(found.ModifiedAt >= before);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        var repo = CreateRepository();
        var found = await repo.GetByIdAsync(ObjectId.GenerateNewId());
        Assert.Null(found);
    }

    [Fact]
    public async Task GetByName_ReturnsDocument()
    {
        var repo = CreateRepository();
        await repo.InsertAsync(new TestItem { Name = "ByName" });

        var found = await repo.GetByNameAsync("ByName");

        Assert.NotNull(found);
        Assert.Equal("ByName", found.Name);
    }

    [Fact]
    public async Task GetByName_NotFound_ReturnsNull()
    {
        var repo = CreateRepository();
        var found = await repo.GetByNameAsync("nonexistent");
        Assert.Null(found);
    }

    [Fact]
    public async Task Upsert_UpdatesExistingDocument()
    {
        var repo = CreateRepository();
        var item = new TestItem { Name = "Original", Quantity = 1 };
        await repo.InsertAsync(item);

        item.Name = "Updated";
        item.Quantity = 99;
        await repo.UpsertAsync(item);

        var found = await repo.GetByIdAsync(item.Id);
        Assert.NotNull(found);
        Assert.Equal("Updated", found.Name);
        Assert.Equal(99, found.Quantity);
    }

    [Fact]
    public async Task Upsert_InsertsWhenDocumentNotFound()
    {
        var repo = CreateRepository();
        var item = new TestItem { Name = "ViaUpsert", Quantity = 5 };

        var result = await repo.UpsertAsync(item);

        Assert.NotNull(result.UpsertedId);
        var found = await repo.GetByIdAsync(item.Id);
        Assert.NotNull(found);
        Assert.Equal("ViaUpsert", found.Name);
    }

    [Fact]
    public async Task Delete_RemovesDocument()
    {
        var repo = CreateRepository();
        var item = new TestItem { Name = "ToDelete" };
        await repo.InsertAsync(item);

        var result = await repo.DeleteAsync(item.Id);

        Assert.Equal(1, result.DeletedCount);
        Assert.Null(await repo.GetByIdAsync(item.Id));
    }

    [Fact]
    public async Task Delete_NonExistentId_ReturnsZeroDeletedCount()
    {
        var repo = CreateRepository();
        var result = await repo.DeleteAsync(ObjectId.GenerateNewId());
        Assert.Equal(0, result.DeletedCount);
    }

    [Fact]
    public async Task GetAll_ReturnsAllDocuments()
    {
        var repo = CreateRepository();
        await repo.InsertAsync(new TestItem { Name = "A" });
        await repo.InsertAsync(new TestItem { Name = "B" });
        await repo.InsertAsync(new TestItem { Name = "C" });

        var all = await repo.GetAllAsync();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task GetAll_EmptyCollection_ReturnsEmptyList()
    {
        var repo = CreateRepository();
        var all = await repo.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task StringId_MatchesObjectIdToString()
    {
        var repo = CreateRepository();
        var item = new TestItem { Name = "StringId" };
        await repo.InsertAsync(item);

        var found = await repo.GetByIdAsync(item.Id);

        Assert.NotNull(found);
        Assert.Equal(found.Id.ToString(), found.StringId);
    }

    [Fact]
    public async Task GetLifetime_ReturnsPositiveTimeSpan()
    {
        var repo = CreateRepository();
        var item = new TestItem { Name = "Lifetime" };
        await repo.InsertAsync(item);

        var found = await repo.GetByIdAsync(item.Id);

        Assert.NotNull(found);
        Assert.True(found.GetLifetime() >= TimeSpan.Zero);
    }
}
