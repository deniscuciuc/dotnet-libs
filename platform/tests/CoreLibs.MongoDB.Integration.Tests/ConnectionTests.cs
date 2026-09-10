using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace CoreLibs.MongoDB.Integration.Tests;

[Collection("MongoDB")]
public class ConnectionTests(MongoDbFixture fixture)
{
    [Fact]
    public void MongoDBProvider_ExposesClientAndDatabase()
    {
        var provider = fixture.CreateProvider("conn_test");

        Assert.NotNull(provider.Client);
        Assert.NotNull(provider.Database);
    }

    [Fact]
    public async Task AddMongoDB_WithOptions_RegistersProviderAndConnection()
    {
        var urlBuilder = new MongoUrlBuilder(fixture.ConnectionString) { DatabaseName = "di_test" };
        if (!string.IsNullOrEmpty(urlBuilder.Username))
            urlBuilder.AuthenticationSource = "admin";

        var services = new ServiceCollection();
        services.AddMongoDB(new MongoDBOptions
        {
            ConnectionString = urlBuilder.ToMongoUrl().ToString()
        });
        var sp = services.BuildServiceProvider();

        var connection = sp.GetRequiredService<IMongoDBConnection>();
        await connection.ConnectAsync();

        var provider = sp.GetRequiredService<IMongoDBProvider>();
        Assert.NotNull(provider.Client);
        Assert.NotNull(provider.Database);
    }

    [Fact]
    public async Task AddMongoDB_WithConfiguration_BindsOptions()
    {
        var urlBuilder = new MongoUrlBuilder(fixture.ConnectionString) { DatabaseName = "di_config_test" };
        if (!string.IsNullOrEmpty(urlBuilder.Username))
            urlBuilder.AuthenticationSource = "admin";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDB:ConnectionString"] = urlBuilder.ToMongoUrl().ToString(),
                ["MongoDB:MinConnectionPoolSize"] = "7",
                ["MongoDB:MaxConnectionPoolSize"] = "21"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMongoDB(configuration);
        var sp = services.BuildServiceProvider();

        var connection = sp.GetRequiredService<IMongoDBConnection>();
        await connection.ConnectAsync();

        var provider = sp.GetRequiredService<IMongoDBProvider>();
        Assert.NotNull(provider.Client);
        Assert.NotNull(provider.Database);
    }

    [Fact]
    public void AddMongoDB_MissingDatabaseName_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddMongoDB(new MongoDBOptions
            {
                ConnectionString = "mongodb://localhost:27017" // no database name segment
            }));
    }

    [Fact]
    public void AddMongoDB_RegistersIMongoDBProviderAsSingleton()
    {
        var urlBuilder = new MongoUrlBuilder(fixture.ConnectionString) { DatabaseName = "di_singleton_test" };
        if (!string.IsNullOrEmpty(urlBuilder.Username))
            urlBuilder.AuthenticationSource = "admin";

        var services = new ServiceCollection();
        services.AddMongoDB(new MongoDBOptions { ConnectionString = urlBuilder.ToMongoUrl().ToString() });
        var sp = services.BuildServiceProvider();

        var a = sp.GetRequiredService<IMongoDBProvider>();
        var b = sp.GetRequiredService<IMongoDBProvider>();

        Assert.Same(a, b);
    }
}
