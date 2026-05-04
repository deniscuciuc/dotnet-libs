using DenisCuciuc.Platform.MongoDB.Actor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB.Integration.Tests;

// --- Basic entity and repository ---

[BsonIgnoreExtraElements]
public class TestItem : EntityBase
{
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
}

public class TestItemIndexes : IndexesBuilder<TestItem>
{
    public TestItemIndexes() => Index(Ascending(x => x.Name)).Unique();
}

public class TestItemRepository(IMongoDBProvider provider)
    : EntityRepository<TestItem>(provider, new TestItemIndexes(), NullLogger<TestItemRepository>.Instance)
{
    public async Task<TestItem?> GetByIdAsync(ObjectId id) =>
        await Find(Filter.Eq(x => x.Id, id)).FirstOrDefaultAsync();

    public async Task<TestItem?> GetByNameAsync(string name) =>
        await Find(Filter.Eq(x => x.Name, name)).FirstOrDefaultAsync();

    public Task InsertAsync(TestItem item) =>
        Primary.InsertOneAsync(item);

    public Task<ReplaceOneResult> UpsertAsync(TestItem item) =>
        Primary.ReplaceOneAsync(
            Filter.Eq(x => x.Id, item.Id),
            item,
            new ReplaceOptions { IsUpsert = true });

    public Task<DeleteResult> DeleteAsync(ObjectId id) =>
        Primary.DeleteOneAsync(Filter.Eq(x => x.Id, id));

    public Task<List<TestItem>> GetAllAsync() =>
        Find(Filter.Empty).ToListAsync();
}

// --- CAS entity and actor for actor tests ---

[BsonIgnoreExtraElements]
public class OrderItem : EntityCas
{
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public string Status { get; set; } = "Pending";
}

public class OrderItemIndexes : IndexesBuilder<OrderItem> { }

public class OrderItemActor(OrderItem entity, ActorContext context, ILogger<OrderItemActor> logger)
    : Actor<OrderItem>(entity, context, logger), IActorResult<OrderItem>
{
    protected override void OnPreProcessing() { }
    protected override void OnPostProcessing() { }

    public void SetStatus(string status) => Entity.Status = status;
    public void SetQuantity(int qty) => Entity.Quantity = qty;
}

public class OrderItemActorFactory(ILogger<OrderItemActor> logger) : IActorFactory<OrderItem, OrderItemActor>
{
    public OrderItemActor Create(OrderItem entity, ActorOwner? owner) =>
        new(entity, new ActorContext(owner, entity.Id.ToString()), logger);
}

public class OrderItemRepository(
    IServiceProvider sp,
    IMongoDBProvider provider,
    IEntityCache<OrderItem> cache,
    IActorFactory<OrderItem, OrderItemActor> factory)
    : ActorRepository<OrderItem, OrderItemActor, IActorResult<OrderItem>>(
        sp, provider, new OrderItemIndexes(), cache, factory, null,
        NullLogger<OrderItemRepository>.Instance);
