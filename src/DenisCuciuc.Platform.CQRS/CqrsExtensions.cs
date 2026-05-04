using System.Reflection;
using DenisCuciuc.Platform.Cache;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DenisCuciuc.Platform.CQRS;

/// <summary>
/// DI registration for CQRS: MediatR, pipeline behaviors, and FluentValidation.
/// </summary>
public static class CqrsExtensions
{
    /// <summary>
    /// Registers CQRS from the assembly containing <typeparamref name="TMarker"/>.
    /// </summary>
    public static IServiceCollection AddPlatformCqrsFromAssemblyOf<TMarker>(this IServiceCollection services)
    {
        return services.AddPlatformCqrs(typeof(TMarker).Assembly);
    }

    /// <summary>
    /// Registers CQRS from the assembly containing <typeparamref name="TMarker"/>,
    /// with explicit options configuration.
    /// </summary>
    public static IServiceCollection AddPlatformCqrsFromAssemblyOf<TMarker>(
        this IServiceCollection services,
        Action<PlatformCqrsOptions> configureOptions)
    {
        return services.AddPlatformCqrs(configureOptions, typeof(TMarker).Assembly);
    }

    /// <summary>
    /// Registers CQRS from the assembly containing <typeparamref name="TMarker"/>,
    /// with options bound from the default section path.
    /// </summary>
    public static IServiceCollection AddPlatformCqrsFromAssemblyOf<TMarker>(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddPlatformCqrs(configuration, typeof(TMarker).Assembly);
    }

    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, and CQRS pipeline behaviors.
    /// </summary>
    public static IServiceCollection AddPlatformCqrs(
        this IServiceCollection services,
        params Assembly[] applicationAssemblies)
    {
        return services.AddPlatformCqrs(_ => { }, applicationAssemblies);
    }

    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, and CQRS pipeline behaviors,
    /// with explicit options configuration.
    /// </summary>
    public static IServiceCollection AddPlatformCqrs(
        this IServiceCollection services,
        Action<PlatformCqrsOptions> configureOptions,
        params Assembly[] applicationAssemblies)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        return services.AddPlatformCqrsCore(
            applicationAssemblies,
            optionsBuilder => optionsBuilder.Configure(configureOptions));
    }

    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, and CQRS pipeline behaviors,
    /// with options bound from the default <c>Cqrs</c> section.
    /// </summary>
    public static IServiceCollection AddPlatformCqrs(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] applicationAssemblies)
    {
        return services.AddPlatformCqrs(configuration, PlatformCqrsOptions.DefaultSectionPath, applicationAssemblies);
    }

    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, and CQRS pipeline behaviors,
    /// with options bound from the provided configuration section path.
    /// </summary>
    public static IServiceCollection AddPlatformCqrs(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath,
        params Assembly[] applicationAssemblies)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionPath);

        return services.AddPlatformCqrsCore(
            applicationAssemblies,
            optionsBuilder => optionsBuilder.Bind(configuration.GetSection(sectionPath)));
    }

    private static IServiceCollection AddPlatformCqrsCore(
        this IServiceCollection services,
        Assembly[] applicationAssemblies,
        Action<OptionsBuilder<PlatformCqrsOptions>> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(applicationAssemblies);
        if (applicationAssemblies.Length == 0)
            throw new ArgumentException("At least one assembly must be provided.", nameof(applicationAssemblies));

        var optionsBuilder = services.AddOptions<PlatformCqrsOptions>();
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
        });

        services.AddValidatorsFromAssemblies(applicationAssemblies, includeInternalTypes: true);

        services.TryAddSingleton<ICacheKeyBuilder, DefaultCacheKeyBuilder>();

        return services;
    }
}
