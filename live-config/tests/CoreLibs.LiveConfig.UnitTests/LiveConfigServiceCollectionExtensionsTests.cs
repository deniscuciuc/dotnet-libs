using CoreLibs.LiveConfig.Startup;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CoreLibs.LiveConfig.UnitTests;

public class LiveConfigServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLiveConfig_ResolvesRegistriesAndPreloader_FromDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLiveConfig(_ => { });
        services.AddSingleton(Substitute.For<IConfigStore>());
        services.AddSingleton<IConfigSource>(new TestConfigSource());
        services.AddSingleton<IConfigApplier>(new TestConfigApplier());
        services.AddSingleton<IConfigHook>(new TestConfigHook());

        using var provider = services.BuildServiceProvider();

        var sourceRegistry = provider.GetRequiredService<ConfigSourceRegistry>();
        var applierRegistry = provider.GetRequiredService<ConfigApplierRegistry>();
        var hookRegistry = provider.GetRequiredService<ConfigHookRegistry>();
        var preloader = provider.GetRequiredService<LiveConfigPreloader>();

        Assert.NotNull(preloader);
        Assert.NotNull(sourceRegistry.Resolve("test-source"));
        Assert.NotNull(applierRegistry.Resolve("test-config"));
        Assert.Single(hookRegistry.All);
    }

    private sealed class TestConfigSource : IConfigSource
    {
        public string SourceName => "test-source";

        public Task<ConfigSnapshot> FetchAsync(string configType, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyDictionary<string, ConfigSnapshot>> FetchAllAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestConfigApplier : IConfigApplier
    {
        public string ConfigType => "test-config";

        public Task ApplyAsync(string json, int version, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestConfigHook : IConfigHook
    {
        public Task OnBeforeApplyAsync(ConfigHookContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task OnAfterApplyAsync(ConfigHookContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
