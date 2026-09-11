using CoreLibs.Localization;
using CoreLibs.Localization.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoreLibs.Localization.UnitTests;

/// <summary>
/// Covers <c>AddCoreLocalization</c>, the only public entry point most consumers touch.
/// </summary>
public sealed class LocalizationRegistrationTests
{
    [Fact]
    public void AddCoreLocalization_ResolvesLocalizerAndCache()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreLocalization();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ILocalizer>());
        Assert.NotNull(provider.GetRequiredService<ILocalizationCache>());
    }

    [Fact]
    public void AddCoreLocalization_RegistersBothAsSingletons()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreLocalization();

        using var provider = services.BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<ILocalizer>(), provider.GetRequiredService<ILocalizer>());
        Assert.Same(
            provider.GetRequiredService<ILocalizationCache>(),
            provider.GetRequiredService<ILocalizationCache>());
    }

    [Fact]
    public void AddCoreLocalization_AppliesConfigureDelegate()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreLocalization(o =>
        {
            o.DefaultCulture = "ro";
            o.FallbackChain = ["ro", "en"];
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LocalizationOptions>>().Value;

        Assert.Equal("ro", options.DefaultCulture);
        Assert.Equal(["ro", "en"], options.FallbackChain);
    }

    [Fact]
    public void AddCoreLocalization_DoesNotOverrideAnExistingRegistration()
    {
        var custom = new LocalizationCache();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILocalizationCache>(custom);
        services.AddCoreLocalization();

        using var provider = services.BuildServiceProvider();

        Assert.Same(custom, provider.GetRequiredService<ILocalizationCache>());
    }

    [Fact]
    public void AddCoreLocalization_IsIdempotent()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreLocalization();
        services.AddCoreLocalization();

        using var provider = services.BuildServiceProvider();

        Assert.Single(provider.GetServices<ILocalizer>());
        Assert.Single(provider.GetServices<ILocalizationCache>());
    }

    [Fact]
    public void ResolvedLocalizer_ReadsFromTheResolvedCache()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreLocalization();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ILocalizationCache>()
            .Update("en", new Dictionary<string, LocalizationValue> { ["hi"] = new() { Value = "Hello" } });

        Assert.Equal("Hello", provider.GetRequiredService<ILocalizer>().Get("hi", "en"));
    }
}
