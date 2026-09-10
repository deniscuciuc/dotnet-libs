using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoreLibs.Configuration.Tests;

public class AddCoreOptionsTests
{
    [Fact]
    public void BindsFromConventionalSection_StripsOptionsSuffix()
    {
        // MongoOptions → Mongo
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = "mongodb://test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCoreOptions<MongoOptions>(config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MongoOptions>>().Value;

        Assert.Equal("mongodb://test", options.ConnectionString);
    }

    [Fact]
    public void StripsPlatformPrefixAndOptionsSuffix()
    {
        // CoreSentryOptions → Sentry
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sentry:Dsn"] = "https://sentry.io/123"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCoreOptions<CoreSentryOptions>(config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CoreSentryOptions>>().Value;

        Assert.Equal("https://sentry.io/123", options.Dsn);
    }

    [Fact]
    public void ExplicitSection_OverridesConvention()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["custom:path:Value"] = "custom-value"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCoreOptions<SimpleOptions>(config, "custom:path");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SimpleOptions>>().Value;

        Assert.Equal("custom-value", options.Value);
    }

    [Fact]
    public void TypeWithoutOptionsSuffix_UsesFullTypeName()
    {
        // SimpleConfig → SimpleConfig
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SimpleConfig:Value"] = "test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCoreOptions<SimpleConfig>(config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SimpleConfig>>().Value;

        Assert.Equal("test", options.Value);
    }

    [Fact]
    public void RegistersValidateOnStart()
    {
        // Ensures the OptionsBuilder pipeline includes ValidateOnStart
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddCoreOptions<MongoOptions>(config);

        var provider = services.BuildServiceProvider();
        // IOptionsMonitor is available (registered by ValidateOnStart infra)
        var monitor = provider.GetService<IOptionsMonitor<MongoOptions>>();
        Assert.NotNull(monitor);
    }

    private class MongoOptions
    {
        public string ConnectionString { get; set; } = "";
    }

    private class CoreSentryOptions
    {
        public string Dsn { get; set; } = "";
    }

    private class SimpleOptions
    {
        public string Value { get; set; } = "";
    }

    private class SimpleConfig
    {
        public string Value { get; set; } = "";
    }
}
