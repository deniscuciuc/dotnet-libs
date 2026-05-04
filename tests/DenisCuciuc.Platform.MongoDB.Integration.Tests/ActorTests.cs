using DenisCuciuc.Platform.MongoDB.Actor;
using DenisCuciuc.Platform.MongoDB.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB.Integration.Tests;

[Collection("MongoDB")]
public class ActorTests(MongoDbFixture fixture)
{
    private OrderItemRepository CreateRepository()
    {
        var provider = fixture.CreateProvider($"actor_{Guid.NewGuid():N}");
        var cache = new MemoryEntityCache<OrderItem>();
        var factory = new OrderItemActorFactory(NullLogger<OrderItemActor>.Instance);
        var sp = new ServiceCollection().BuildServiceProvider();
        return new OrderItemRepository(sp, provider, cache, factory);
    }

    [Fact]
    public async Task Execute_UpdatesEntityFieldInMemory()
    {
        var repo = CreateRepository();
        var item = new OrderItem { Name = "Widget", Status = "Pending" };
        await repo.Primary.InsertOneAsync(item);

        var result = await repo.Actor.ExecuteAsync(item.Id, actor => actor.SetStatus("Active"));

        Assert.Equal("Active", result.Entity.Status);
    }

    [Fact]
    public async Task Execute_PersistsChangesToDatabase()
    {
        var repo = CreateRepository();
        var item = new OrderItem { Name = "Persist" };
        await repo.Primary.InsertOneAsync(item);

        await repo.Actor.ExecuteAsync(item.Id, actor => actor.SetQuantity(42));

        var reloaded = await repo.Primary.Find(x => x.Id == item.Id).FirstOrDefaultAsync();
        Assert.NotNull(reloaded);
        Assert.Equal(42, reloaded.Quantity);
    }

    [Fact]
    public async Task Execute_IncrementsCasFieldOnEachSave()
    {
        var repo = CreateRepository();
        var item = new OrderItem { Name = "CasTest" };
        await repo.Primary.InsertOneAsync(item);

        Assert.Equal(0, item.Cas);

        await repo.Actor.ExecuteAsync(item.Id, actor => actor.SetStatus("Step1"));
        var after1 = await repo.Primary.Find(x => x.Id == item.Id).FirstOrDefaultAsync();
        Assert.NotNull(after1);
        Assert.Equal(1, after1.Cas);

        await repo.Actor.ExecuteAsync(item.Id, actor => actor.SetStatus("Step2"));
        var after2 = await repo.Primary.Find(x => x.Id == item.Id).FirstOrDefaultAsync();
        Assert.NotNull(after2);
        Assert.Equal(2, after2.Cas);
    }

    [Fact]
    public async Task Execute_AppliesMultipleFieldMutations()
    {
        var repo = CreateRepository();
        var item = new OrderItem { Name = "Multi" };
        await repo.Primary.InsertOneAsync(item);

        await repo.Actor.ExecuteAsync(item.Id, actor =>
        {
            actor.SetStatus("Shipped");
            actor.SetQuantity(10);
        });

        var reloaded = await repo.Primary.Find(x => x.Id == item.Id).FirstOrDefaultAsync();
        Assert.NotNull(reloaded);
        Assert.Equal("Shipped", reloaded.Status);
        Assert.Equal(10, reloaded.Quantity);
    }

    [Fact]
    public async Task Execute_NotFound_ThrowsEntityNotFoundException()
    {
        var repo = CreateRepository();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            repo.Actor.ExecuteAsync(ObjectId.GenerateNewId(), actor => actor.SetStatus("X")));
    }

    [Fact]
    public async Task ExecuteSafe_NotFound_ReturnsExceptionInResult()
    {
        var repo = CreateRepository();

        var result = await repo.Actor.ExecuteSafeAsync(
            ObjectId.GenerateNewId(), actor => actor.SetStatus("X"));

        Assert.NotNull(result.Exception);
        Assert.IsType<EntityNotFoundException>(result.Exception);
    }

    [Fact]
    public async Task ExecuteSafe_Success_ReturnsResultWithNoException()
    {
        var repo = CreateRepository();
        var item = new OrderItem { Name = "Safe" };
        await repo.Primary.InsertOneAsync(item);

        var result = await repo.Actor.ExecuteSafeAsync(item.Id, actor => actor.SetStatus("Done"));

        Assert.Null(result.Exception);
        Assert.Equal("Done", result.Actor.Entity.Status);
    }

    [Fact]
    public async Task TryFindAsync_ReturnsEntity_WhenExists()
    {
        var repo = CreateRepository();
        var item = new OrderItem { Name = "Findable" };
        await repo.Primary.InsertOneAsync(item);

        var found = await repo.Actor.TryFindAsync(item.Id);

        Assert.NotNull(found);
        Assert.Equal("Findable", found.Name);
    }

    [Fact]
    public async Task TryFindAsync_ReturnsNull_WhenNotExists()
    {
        var repo = CreateRepository();

        var found = await repo.Actor.TryFindAsync(ObjectId.GenerateNewId());

        Assert.Null(found);
    }

    [Fact]
    public async Task ActivatedByOwner_TrueWhenOwnerIdMatchesEntity()
    {
        var repo = CreateRepository();
        var item = new OrderItem { Name = "OwnedItem" };
        await repo.Primary.InsertOneAsync(item);

        // Build a separate repo with an owner whose Id matches the entity Id
        var provider = fixture.CreateProvider($"actor_{Guid.NewGuid():N}");
        var cache = new MemoryEntityCache<OrderItem>();
        var factory = new OrderItemActorFactory(NullLogger<OrderItemActor>.Instance);
        var sp = new ServiceCollection()
            .AddSingleton(new ActorOwner(item.Id))
            .BuildServiceProvider();
        var ownedRepo = new OrderItemRepository(sp, provider, cache, factory);

        await ownedRepo.Primary.InsertOneAsync(item);

        var result = await ownedRepo.Actor.ExecuteAsync(item.Id, actor =>
        {
            Assert.True(actor.ActivatedByOwner);
        });

        Assert.True(result.ActivatedByOwner);
    }
}
