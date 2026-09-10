namespace CoreLibs.MongoDB.Integration.Tests;

[Collection("MongoDB")]
public class CounterTests(MongoDbFixture fixture)
{
    private TestItemRepository CreateRepository() =>
        new(fixture.CreateProvider($"ctr_{Guid.NewGuid():N}"));

    [Fact]
    public async Task IncrementAsync_ReturnsSequentialValues()
    {
        var repo = CreateRepository();

        var v1 = await repo.IncrementAsync();
        var v2 = await repo.IncrementAsync();
        var v3 = await repo.IncrementAsync();

        Assert.Equal(1, v1);
        Assert.Equal(2, v2);
        Assert.Equal(3, v3);
    }

    [Fact]
    public async Task IncrementAsync_StartsAtOne()
    {
        var repo = CreateRepository();
        var first = await repo.IncrementAsync();
        Assert.Equal(1, first);
    }

    [Fact]
    public async Task IncrementAsync_WithSuffix_IsSeparateCounter()
    {
        var repo = CreateRepository();

        var base1 = await repo.IncrementAsync();
        var suffixed1 = await repo.IncrementAsync("_daily");
        var base2 = await repo.IncrementAsync();
        var suffixed2 = await repo.IncrementAsync("_daily");

        Assert.Equal(1, base1);
        Assert.Equal(1, suffixed1);
        Assert.Equal(2, base2);
        Assert.Equal(2, suffixed2);
    }

    [Fact]
    public async Task IncrementAsync_TwoInstancesSameProvider_ShareCounter()
    {
        var provider = fixture.CreateProvider($"ctr_shared_{Guid.NewGuid():N}");
        var repo1 = new TestItemRepository(provider);
        var repo2 = new TestItemRepository(provider);

        // Same collection → same entity type → same counter key
        var v1 = await repo1.IncrementAsync();
        var v2 = await repo2.IncrementAsync();

        Assert.Equal(1, v1);
        Assert.Equal(2, v2);
    }

    [Fact]
    public async Task IncrementAsync_DifferentDatabases_IndependentCounters()
    {
        var repoA = new TestItemRepository(fixture.CreateProvider($"ctr_a_{Guid.NewGuid():N}"));
        var repoB = new TestItemRepository(fixture.CreateProvider($"ctr_b_{Guid.NewGuid():N}"));

        var vA = await repoA.IncrementAsync();
        var vB = await repoB.IncrementAsync();

        Assert.Equal(1, vA);
        Assert.Equal(1, vB);
    }
}
