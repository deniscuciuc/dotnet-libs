using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DenisCuciuc.Platform.MQ;

public static class MqExtensions
{
    public static IServiceCollection AddPlatformRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IMqConsumerConfigurator>? configureConsumers = null,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configureBus = null,
        string sectionPath = PlatformRabbitMqOptions.DefaultSectionPath
    )
    {
        services
            .AddOptions<PlatformRabbitMqOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services.AddPlatformRabbitMqCore(configureConsumers, configureBus);
    }

    public static IServiceCollection AddPlatformRabbitMq(
        this IServiceCollection services,
        Action<PlatformRabbitMqOptions> configureOptions,
        Action<IMqConsumerConfigurator>? configureConsumers = null,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configureBus = null
    )
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        services
            .AddOptions<PlatformRabbitMqOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services.AddPlatformRabbitMqCore(configureConsumers, configureBus);
    }

    private static IServiceCollection AddPlatformRabbitMqCore(
        this IServiceCollection services,
        Action<IMqConsumerConfigurator>? configureConsumers,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configureBus
    )
    {
        services.AddMassTransit(mt =>
        {
            var impl = new MqConsumerConfigurator(mt);
            configureConsumers?.Invoke(impl);

            mt.UsingRabbitMq((ctx, cfg) =>
            {
                var options = ctx
                    .GetRequiredService<IOptionsMonitor<PlatformRabbitMqOptions>>()
                    .CurrentValue;

                cfg.Host(options.Host, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                impl.Configure(ctx, cfg, options);

                configureBus?.Invoke(ctx, cfg);

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
