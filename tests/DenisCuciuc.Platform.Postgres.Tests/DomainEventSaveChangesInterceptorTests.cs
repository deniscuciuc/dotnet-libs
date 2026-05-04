using DenisCuciuc.Platform.Domain;
using DenisCuciuc.Platform.Postgres;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace DenisCuciuc.Platform.Postgres.Tests;

public class DomainEventSaveChangesInterceptorTests
{
    private class TestItemEntity : Entity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed record TestDomainEvent : DomainEvent;

    private class TestDbContext : DbContext
    {
        public DbSet<TestItemEntity> Items { get; set; } = null!;

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestItemEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<TestItemEntity>().Ignore(x => x.DomainEvents);
        }
    }

    private static TestDbContext CreateDbContext(DbContextOptions<TestDbContext>? options = null)
    {
        options ??= new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid():N}")
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task SavedChangesAsync_DispatchesDomainEvents_ForEntitiesWithEvents()
    {
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        var interceptor = new DomainEventSaveChangesInterceptor(dispatcher);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"dispatch-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;

        await using var ctx = CreateDbContext(dbOptions);
        await ctx.Database.EnsureCreatedAsync();

        var entity = new TestItemEntity { Name = "event-test" };
        entity.AddDomainEvent(new TestDomainEvent());
        ctx.Items.Add(entity);

        await ctx.SaveChangesAsync();

        await dispatcher.Received(1).DispatchEventsAsync(
            Arg.Is<IEnumerable<Entity>>(e => e.Any(x => x.Id == entity.Id)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_DoesNotDispatch_WhenNoDomainEvents()
    {
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        var interceptor = new DomainEventSaveChangesInterceptor(dispatcher);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"no-events-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;

        await using var ctx = CreateDbContext(dbOptions);
        await ctx.Database.EnsureCreatedAsync();

        var entity = new TestItemEntity { Name = "no-events" };
        ctx.Items.Add(entity);

        await ctx.SaveChangesAsync();

        await dispatcher.DidNotReceive().DispatchEventsAsync(
            Arg.Any<IEnumerable<Entity>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_DispatchesMultipleEntities_WhenMultipleHaveEvents()
    {
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        var interceptor = new DomainEventSaveChangesInterceptor(dispatcher);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"multi-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;

        await using var ctx = CreateDbContext(dbOptions);
        await ctx.Database.EnsureCreatedAsync();

        var entity1 = new TestItemEntity { Name = "e1" };
        var entity2 = new TestItemEntity { Name = "e2" };
        entity1.AddDomainEvent(new TestDomainEvent());
        entity2.AddDomainEvent(new TestDomainEvent());
        ctx.Items.AddRange(entity1, entity2);

        await ctx.SaveChangesAsync();

        await dispatcher.Received(1).DispatchEventsAsync(
            Arg.Is<IEnumerable<Entity>>(e => e.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_DoesNotDispatch_WhenContextIsEmpty()
    {
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        var interceptor = new DomainEventSaveChangesInterceptor(dispatcher);

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"empty-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;

        await using var ctx = CreateDbContext(dbOptions);
        await ctx.Database.EnsureCreatedAsync();
        await ctx.SaveChangesAsync();

        await dispatcher.DidNotReceive().DispatchEventsAsync(
            Arg.Any<IEnumerable<Entity>>(),
            Arg.Any<CancellationToken>());
    }
}
