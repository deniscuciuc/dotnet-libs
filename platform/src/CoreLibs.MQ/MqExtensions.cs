using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoreLibs.MQ;

public static class MqExtensions
{
    public static IServiceCollection AddCoreRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IMqConsumerConfigurator>? configureConsumers = null,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configureBus = null,
        string sectionPath = CoreRabbitMqOptions.DefaultSectionPath
    )
    {
        services
            .AddOptions<CoreRabbitMqOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services.AddCoreRabbitMqCore(configureConsumers, configureBus);
    }

    public static IServiceCollection AddCoreRabbitMq(
        this IServiceCollection services,
        Action<CoreRabbitMqOptions> configureOptions,
        Action<IMqConsumerConfigurator>? configureConsumers = null,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configureBus = null
    )
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        services
            .AddOptions<CoreRabbitMqOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services.AddCoreRabbitMqCore(configureConsumers, configureBus);
    }

    private static IServiceCollection AddCoreRabbitMqCore(
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
                    .GetRequiredService<IOptionsMonitor<CoreRabbitMqOptions>>()
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
