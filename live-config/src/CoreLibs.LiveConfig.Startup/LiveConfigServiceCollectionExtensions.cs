using System.Reflection;
using CoreLibs.LiveConfig.Observability;
using CoreLibs.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig.Startup;

public static class LiveConfigServiceCollectionExtensions
{
    /// <summary>
    /// Registers LiveConfig core services (engine, registries, metrics, consumer state).
    /// </summary>
    public static IServiceCollection AddLiveConfig(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = LiveConfigOptions.SectionPath)
    {
        services.Configure<LiveConfigOptions>(configuration.GetSection(sectionPath));
        var retention = configuration.GetSection($"{sectionPath}:retention").Get<ConfigRetentionOptions>()
                        ?? new ConfigRetentionOptions();
        services.TryAddSingleton(retention);
        RegisterCoreServices(services);
        return services;
    }

    /// <summary>
    /// Registers LiveConfig core services with inline options.
    /// </summary>
    public static IServiceCollection AddLiveConfig(
        this IServiceCollection services,
        Action<LiveConfigOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<LiveConfigOptions>(_ => { });

        services.TryAddSingleton(new ConfigRetentionOptions());
        RegisterCoreServices(services);
        return services;
    }

    /// <summary>
    /// Registers a config source.
    /// </summary>
    public static IServiceCollection AddLiveConfigSource<TSource>(this IServiceCollection services)
        where TSource : class, IConfigSource
    {
        services.AddSingleton<IConfigSource, TSource>();
        return services;
    }

    /// <summary>
    /// Registers a config applier.
    /// </summary>
    public static IServiceCollection AddLiveConfigApplier<TApplier>(this IServiceCollection services)
        where TApplier : class, IConfigApplier
    {
        services.AddSingleton<IConfigApplier, TApplier>();
        return services;
    }

    /// <summary>
    /// Registers a config hook.
    /// </summary>
    public static IServiceCollection AddLiveConfigHook<THook>(this IServiceCollection services)
        where THook : class, IConfigHook
    {
        services.AddSingleton<IConfigHook, THook>();
        return services;
    }

    /// <summary>
    /// Registers a typed config cache and its corresponding cache applier.
    /// Consumers inject <c>IConfigCache&lt;IReadOnlyList&lt;TDto&gt;&gt;</c> to access the latest data.
    /// </summary>
    public static IServiceCollection AddLiveConfigCachedConfig<TDto>(
        this IServiceCollection services,
        string configType) where TDto : class
    {
        services.AddSingleton<IConfigCache<IReadOnlyList<TDto>>, ConfigCache<IReadOnlyList<TDto>>>();

        services.AddSingleton<IConfigApplier>(sp =>
        {
            var cache = sp.GetRequiredService<IConfigCache<IReadOnlyList<TDto>>>();
            var applierLogger = sp.GetRequiredService<ILogger<CacheConfigApplier<TDto>>>();
            return new CacheConfigApplier<TDto>(cache, configType, applierLogger);
        });

        return services;
    }

    /// <summary>
    /// Auto-discovers and registers all <see cref="IConfigSource"/> implementations from the assembly containing <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddLiveConfigSourcesFromAssemblyOf<T>(this IServiceCollection services)
    {
        return AddFromAssembly<IConfigSource>(services, typeof(T).Assembly);
    }

    /// <summary>
    /// Auto-discovers and registers all <see cref="IConfigApplier"/> implementations from the assembly containing <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddLiveConfigAppliersFromAssemblyOf<T>(this IServiceCollection services)
    {
        return AddFromAssembly<IConfigApplier>(services, typeof(T).Assembly);
    }

    /// <summary>
    /// Auto-discovers and registers all <see cref="IConfigHook"/> implementations from the assembly containing <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddLiveConfigHooksFromAssemblyOf<T>(this IServiceCollection services)
    {
        return AddFromAssembly<IConfigHook>(services, typeof(T).Assembly);
    }

    /// <summary>
    /// Adds the preload background service that loads critical configs on startup.
    /// </summary>
    public static IServiceCollection AddLiveConfigPreloadService(this IServiceCollection services)
    {
        services.AddHostedService<LiveConfigPreloadService>();
        return services;
    }

    /// <summary>
    /// Registers the LiveConfig CoreLibs startup task for preloading critical configs
    /// during <c>RunWithStartupAsync()</c>, plus a startup guard for <see cref="LiveConfigEngine"/>.
    /// Also registers the polling fallback background service.
    /// </summary>
    public static IServiceCollection AddCoreLiveConfigStartup(this IServiceCollection services)
    {
        services.AddCoreStartup<LiveConfigPreloadStartup>();
        services.AddCoreStartupGuard<LiveConfigEngine>();
        services.AddHostedService<LiveConfigPreloadService>();
        return services;
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.TryAddSingleton(sp =>
        {
            var registry = new ConfigSourceRegistry();
            foreach (var source in sp.GetServices<IConfigSource>())
                registry.Register(source);
            return registry;
        });

        services.TryAddSingleton(sp =>
        {
            var registry = new ConfigApplierRegistry();
            foreach (var applier in sp.GetServices<IConfigApplier>())
                registry.Register(applier);
            return registry;
        });

        services.TryAddSingleton(sp =>
        {
            var registry = new ConfigHookRegistry();
            foreach (var hook in sp.GetServices<IConfigHook>())
                registry.Register(hook);
            return registry;
        });

        services.TryAddSingleton<LiveConfigEngine>();
        services.TryAddSingleton<LiveConfigPreloader>();
        services.TryAddSingleton<LiveConfigConsumerState>();
        services.TryAddSingleton<LiveConfigMetrics>();
    }

    private static IServiceCollection AddFromAssembly<TInterface>(IServiceCollection services, Assembly assembly)
    {
        var interfaceType = typeof(TInterface);
        var types = SafeGetTypes(assembly)
            .Where(t => t is { IsAbstract: false, IsInterface: false } && interfaceType.IsAssignableFrom(t));

        foreach (var type in types)
            services.AddSingleton(interfaceType, type);

        return services;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try
        {
            return a.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }
}
