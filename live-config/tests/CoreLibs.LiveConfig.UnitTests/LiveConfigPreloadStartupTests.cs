using CoreLibs.LiveConfig.Startup;
using CoreLibs.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CoreLibs.LiveConfig.UnitTests;

public class LiveConfigPreloadStartupTests
{
    [Fact]
    public async Task StartupAsync_SetsConsumerStateAndMarksConfigured_WhenPreloadDisabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLiveConfig(options =>
        {
            options.EnablePreload = false;
        });
        services.AddCoreStartupGuard<LiveConfigEngine>();

        await using var provider = services.BuildServiceProvider();
        var host = new TestHostStartup(provider);

        var startup = new LiveConfigPreloadStartup();
        await startup.StartupAsync(host);

        Assert.True(provider.GetRequiredService<LiveConfigConsumerState>().IsReady);
        await provider.GetRequiredService<StartupGuard<LiveConfigEngine>>().StartupAsync(host);
    }

    [Fact]
    public async Task StartupAsync_SetsConsumerStateAndMarksConfigured_WhenPreloadSucceeds()
    {
        var store = Substitute.For<IConfigStore>();
        store.GetActiveAsync("bonuses", Arg.Any<CancellationToken>()).Returns((ConfigVersion?)null);
        store.GetActiveHashAsync("bonuses", Arg.Any<CancellationToken>()).Returns((string?)null);
        store.CreateVersionAsync("bonuses", Arg.Any<string>(), Arg.Any<string>(), "startup-preload",
                Arg.Any<CancellationToken>())
            .Returns(1);
        store.ActivateVersionAsync("bonuses", 1, Arg.Any<CancellationToken>()).Returns(true);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(store);
        services.AddLiveConfig(options =>
        {
            options.EnablePreload = true;
            options.CriticalConfigTypes = ["bonuses"];
            options.Retry = new RetryOptions { MaxRetries = 0 };
        });
        services.AddSingleton<IConfigSource>(new TestConfigSource());
        services.AddSingleton<IConfigApplier>(new TestConfigApplier());
        services.AddCoreStartupGuard<LiveConfigEngine>();

        await using var provider = services.BuildServiceProvider();
        var host = new TestHostStartup(provider);

        var startup = new LiveConfigPreloadStartup();
        await startup.StartupAsync(host);

        Assert.True(provider.GetRequiredService<LiveConfigConsumerState>().IsReady);
        await provider.GetRequiredService<StartupGuard<LiveConfigEngine>>().StartupAsync(host);
        await store.Received().CreateVersionAsync(
            "bonuses", "[{\"name\":\"Welcome Bonus\"}]", Arg.Any<string>(), "startup-preload",
            Arg.Any<CancellationToken>());
    }

    private sealed class TestHostStartup(IServiceProvider services) : IHostStartup
    {
        public IServiceProvider Services { get; } = services;
        public IConfiguration Configuration { get; } = new ConfigurationBuilder().Build();
        public IHostEnvironment Environment { get; } = new TestHostEnvironment();
        public ILogger Logger { get; } = services.GetRequiredService<ILoggerFactory>().CreateLogger("test");
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "CoreLibs.LiveConfig.UnitTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class TestConfigSource : IConfigSource
    {
        public string SourceName => "gsheet";

        public Task<ConfigSnapshot> FetchAsync(string configType, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ConfigSnapshot(
                configType,
                "[{\"name\":\"Welcome Bonus\"}]",
                ConfigHasher.ComputeHash("[{\"name\":\"Welcome Bonus\"}]"),
                DateTimeOffset.UtcNow));
        }

        public Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchAllAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestConfigApplier : IConfigApplier
    {
        public string ConfigType => "bonuses";

        public Task ApplyAsync(string json, int version, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
