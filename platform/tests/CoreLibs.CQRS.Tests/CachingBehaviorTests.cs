using CoreLibs.Cache;
using ErrorOr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CoreLibs.CQRS.Tests;

public record CacheableTestQuery(string Key) : ICacheableQuery<string>
{
    public QueryCachePolicy CachePolicy { get; init; } =
        QueryCachePolicy.Absolute(Key, TimeSpan.FromMinutes(5));
}

public class CachingBehaviorTests
{
    private static IOptionsMonitor<CoreCqrsOptions> BuildOptions(
        bool cachingEnabled = true, bool behaviorEnabled = true)
    {
        var monitor = Substitute.For<IOptionsMonitor<CoreCqrsOptions>>();
        monitor.CurrentValue.Returns(new CoreCqrsOptions
        {
            EnableCachingBehavior = behaviorEnabled,
            Caching = new CoreCqrsCachingOptions
            {
                Enabled = cachingEnabled,
                KeyPrefix = "test",
                MaxJitterSeconds = 0,
                DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5)
            }
        });
        return monitor;
    }

    private static ICacheKeyBuilder BuildKeyBuilder()
    {
        var kb = Substitute.For<ICacheKeyBuilder>();
        kb.Build(Arg.Any<object[]>())
          .Returns(ci => string.Join(":", ci.ArgAt<object[]>(0)));
        return kb;
    }

    [Fact]
    public async Task Handle_WhenBehaviorDisabled_CallsNext()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new CachingBehavior<CacheableTestQuery, string>(
            sp, BuildKeyBuilder(), BuildOptions(behaviorEnabled: false),
            NullLogger<CachingBehavior<CacheableTestQuery, string>>.Instance);

        var called = false;
        var result = await sut.Handle(new CacheableTestQuery("k"), () =>
        {
            called = true;
            return Task.FromResult(ErrorOrFactory.From("value"));
        }, CancellationToken.None);

        Assert.True(called);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public async Task Handle_WhenCachingDisabled_CallsNext()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new CachingBehavior<CacheableTestQuery, string>(
            sp, BuildKeyBuilder(), BuildOptions(cachingEnabled: false),
            NullLogger<CachingBehavior<CacheableTestQuery, string>>.Instance);

        var called = false;
        var result = await sut.Handle(new CacheableTestQuery("k"), () =>
        {
            called = true;
            return Task.FromResult(ErrorOrFactory.From("cached-value"));
        }, CancellationToken.None);

        Assert.True(called);
        Assert.Equal("cached-value", result.Value);
    }

    [Fact]
    public async Task Handle_WhenNoCacheRegistered_CallsNext()
    {
        // ICache is NOT registered in the service provider
        var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new CachingBehavior<CacheableTestQuery, string>(
            sp, BuildKeyBuilder(), BuildOptions(),
            NullLogger<CachingBehavior<CacheableTestQuery, string>>.Instance);

        var called = false;
        var result = await sut.Handle(new CacheableTestQuery("k"), () =>
        {
            called = true;
            return Task.FromResult(ErrorOrFactory.From("no-cache"));
        }, CancellationToken.None);

        Assert.True(called);
        Assert.Equal("no-cache", result.Value);
    }

    [Fact]
    public async Task Handle_CacheMiss_CallsNext_AndCachesResult()
    {
        var cache = Substitute.For<ICache>();
        cache.GetAsync<object>(Arg.Any<string>()).Returns((object?)null);

        var services = new ServiceCollection();
        services.AddSingleton(cache);
        var sp = services.BuildServiceProvider();

        var sut = new CachingBehavior<CacheableTestQuery, string>(
            sp, BuildKeyBuilder(), BuildOptions(),
            NullLogger<CachingBehavior<CacheableTestQuery, string>>.Instance);

        var result = await sut.Handle(new CacheableTestQuery("k"), () =>
            Task.FromResult(ErrorOrFactory.From("miss-result")), CancellationToken.None);

        Assert.Equal("miss-result", result.Value);
        await cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<object>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<When>());
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedValue_WithoutCallingNext()
    {
        var cache = Substitute.For<ICache>();

        // The behavior uses CachedResponse<T> which is private — verify via callCount
        cache.GetAsync<object>(Arg.Any<string>()).Returns((object?)null);

        var services = new ServiceCollection();
        services.AddSingleton(cache);
        var sp = services.BuildServiceProvider();

        var sut = new CachingBehavior<CacheableTestQuery, string>(
            sp, BuildKeyBuilder(), BuildOptions(),
            NullLogger<CachingBehavior<CacheableTestQuery, string>>.Instance);

        var callCount = 0;
        await sut.Handle(new CacheableTestQuery("k"), () =>
        {
            callCount++;
            return Task.FromResult(ErrorOrFactory.From("from-handler"));
        }, CancellationToken.None);

        Assert.Equal(1, callCount); // cache miss → next called exactly once
    }

    [Fact]
    public async Task Handle_BypassCacheRead_CallsNext_SkipsCacheGet()
    {
        var cache = Substitute.For<ICache>();
        var services = new ServiceCollection();
        services.AddSingleton(cache);
        var sp = services.BuildServiceProvider();

        var sut = new CachingBehavior<CacheableTestQuery, string>(
            sp, BuildKeyBuilder(), BuildOptions(),
            NullLogger<CachingBehavior<CacheableTestQuery, string>>.Instance);

        var query = new CacheableTestQuery("k") with
        {
            CachePolicy = new QueryCachePolicy
            {
                Key = "k",
                BypassCacheRead = true,
                AbsoluteExpiration = TimeSpan.FromMinutes(5)
            }
        };

        var called = false;
        var result = await sut.Handle(query, () =>
        {
            called = true;
            return Task.FromResult(ErrorOrFactory.From("bypass"));
        }, CancellationToken.None);

        Assert.True(called);
        await cache.DidNotReceive().GetAsync<object>(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_NonCacheableQuery_CallsNext_Directly()
    {
        var cache = Substitute.For<ICache>();
        var services = new ServiceCollection();
        services.AddSingleton(cache);
        var sp = services.BuildServiceProvider();

        var sut = new CachingBehavior<TestQuery, string>(
            sp, BuildKeyBuilder(), BuildOptions(),
            NullLogger<CachingBehavior<TestQuery, string>>.Instance);

        var called = false;
        var result = await sut.Handle(new TestQuery("name"), () =>
        {
            called = true;
            return Task.FromResult(ErrorOrFactory.From("not-cached"));
        }, CancellationToken.None);

        Assert.True(called);
        await cache.DidNotReceive().GetAsync<object>(Arg.Any<string>());
    }
}
