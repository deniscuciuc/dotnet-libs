using DenisCuciuc.Platform.Cache;
using DenisCuciuc.Platform.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.Redis;

namespace DenisCuciuc.Platform.Redis.Integration.Tests;

public class RedisFixture : IAsyncLifetime
{
    private readonly Testcontainers.Redis.RedisContainer _container =
        new Testcontainers.Redis.RedisBuilder("redis:7-alpine")
            .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("Redis")]
public class RedisCollection : ICollectionFixture<RedisFixture>;
