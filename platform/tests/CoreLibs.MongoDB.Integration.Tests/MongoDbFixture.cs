using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace CoreLibs.MongoDB.Integration.Tests;

public class MongoDbFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();

    public string ConnectionString => _container.GetConnectionString();

    public IMongoDBProvider CreateProvider(string databaseName)
    {
        var urlBuilder = new MongoUrlBuilder(ConnectionString) { DatabaseName = databaseName };
        if (!string.IsNullOrEmpty(urlBuilder.Username))
            urlBuilder.AuthenticationSource = "admin";
        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
        var provider = new MongoDBProvider(databaseName, settings);
        provider.ConnectAsync().GetAwaiter().GetResult();
        return provider;
    }

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition("MongoDB")]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>;
