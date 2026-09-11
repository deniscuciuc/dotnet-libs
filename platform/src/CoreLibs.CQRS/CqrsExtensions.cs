using System.Reflection;
using CoreLibs.Cache;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CoreLibs.CQRS;

/// <summary>
/// DI registration for CQRS: MediatR, pipeline behaviors, and FluentValidation.
/// </summary>
public static class CqrsExtensions
{
    /// <summary>
    /// Registers CQRS from the assembly containing <typeparamref name="TMarker"/>.
    /// </summary>
    public static IServiceCollection AddCoreCqrsFromAssemblyOf<TMarker>(this IServiceCollection services)
    {
        return services.AddCoreCqrs(typeof(TMarker).Assembly);
    }

    /// <summary>
    /// Registers CQRS from the assembly containing <typeparamref name="TMarker"/>,
    /// with explicit options configuration.
    /// </summary>
    public static IServiceCollection AddCoreCqrsFromAssemblyOf<TMarker>(
        this IServiceCollection services,
        Action<CoreCqrsOptions> configureOptions)
    {
        return services.AddCoreCqrs(configureOptions, typeof(TMarker).Assembly);
    }

    /// <summary>
    /// Registers CQRS from the assembly containing <typeparamref name="TMarker"/>,
    /// with options bound from the default section path.
    /// </summary>
    public static IServiceCollection AddCoreCqrsFromAssemblyOf<TMarker>(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddCoreCqrs(configuration, typeof(TMarker).Assembly);
    }

    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, and CQRS pipeline behaviors.
    /// </summary>
    public static IServiceCollection AddCoreCqrs(
        this IServiceCollection services,
        params Assembly[] applicationAssemblies)
    {
        return services.AddCoreCqrs(_ => { }, applicationAssemblies);
    }

    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, and CQRS pipeline behaviors,
    /// with explicit options configuration.
    /// </summary>
    public static IServiceCollection AddCoreCqrs(
        this IServiceCollection services,
        Action<CoreCqrsOptions> configureOptions,
        params Assembly[] applicationAssemblies)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        return services.AddCoreCqrsCore(
            applicationAssemblies,
            optionsBuilder => optionsBuilder.Configure(configureOptions));
    }

    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, and CQRS pipeline behaviors,
    /// with options bound from the default <c>Cqrs</c> section.
    /// </summary>
    public static IServiceCollection AddCoreCqrs(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] applicationAssemblies)
    {
        return services.AddCoreCqrs(configuration, CoreCqrsOptions.DefaultSectionPath, applicationAssemblies);
    }

    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, and CQRS pipeline behaviors,
    /// with options bound from the provided configuration section path.
    /// </summary>
    public static IServiceCollection AddCoreCqrs(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath,
        params Assembly[] applicationAssemblies)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionPath);

        return services.AddCoreCqrsCore(
            applicationAssemblies,
            optionsBuilder => optionsBuilder.Bind(configuration.GetSection(sectionPath)));
    }

    private static IServiceCollection AddCoreCqrsCore(
        this IServiceCollection services,
        Assembly[] applicationAssemblies,
        Action<OptionsBuilder<CoreCqrsOptions>> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(applicationAssemblies);
        if (applicationAssemblies.Length == 0)
            throw new ArgumentException("At least one assembly must be provided.", nameof(applicationAssemblies));

        var optionsBuilder = services.AddOptions<CoreCqrsOptions>();
        configureOptions(optionsBuilder);
        optionsBuilder
            .ValidateDataAnnotations()
            .Validate(static options => options.Caching.DefaultAbsoluteExpiration is null || options.Caching.DefaultAbsoluteExpiration > TimeSpan.Zero,
                "Caching default absolute expiration must be null or greater than zero.")
            .Validate(static options => options.Caching.MaxJitterSeconds >= 0,
                "Caching jitter must be greater than or equal to zero.")
            .ValidateOnStart();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(applicationAssemblies);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblies(applicationAssemblies, includeInternalTypes: true);

        services.TryAddSingleton<ICacheKeyBuilder, DefaultCacheKeyBuilder>();

        return services;
    }
}
