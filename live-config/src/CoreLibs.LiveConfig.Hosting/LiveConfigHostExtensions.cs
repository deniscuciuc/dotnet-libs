using CoreLibs.LiveConfig.Hosting.Consumers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.LiveConfig.Hosting;

public static class LiveConfigHostExtensions
{
    /// <summary>
    /// Registers LiveConfig hosting services (MQ consumers, Redis subscriber).
    /// </summary>
    public static IServiceCollection AddLiveConfigHosting(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = LiveConfigHostOptions.SectionPath)
    {
        services.Configure<LiveConfigHostOptions>(configuration.GetSection(sectionPath));

        var options = configuration.GetSection(sectionPath).Get<LiveConfigHostOptions>()
                      ?? new LiveConfigHostOptions();

        if (options.EnableRedisSubscriber)
            services.AddHostedService<LiveConfigSubscriberService>();

        return services;
    }

    /// <summary>
    /// Configures MassTransit with LiveConfig consumers.
    /// Call inside your <c>AddMassTransit</c> configuration.
    /// </summary>
    public static void AddLiveConfigConsumers(
        this IBusRegistrationConfigurator configurator,
        LiveConfigHostOptions? options = null)
    {
        options ??= new LiveConfigHostOptions();

        if (!options.EnableMqConsumers)
            return;

        configurator.AddConsumer<ConfigImportRequestConsumer>();
        configurator.AddConsumer<ConfigRollbackRequestConsumer>();
    }

    /// <summary>
    /// Configures MassTransit endpoint conventions for LiveConfig consumers.
    /// Call inside your endpoint configuration.
    /// </summary>
    public static void ConfigureLiveConfigEndpoints(
        this IBusRegistrationContext context,
        IRabbitMqBusFactoryConfigurator cfg,
        LiveConfigHostOptions? options = null)
    {
        options ??= new LiveConfigHostOptions();

        if (!options.EnableMqConsumers)
            return;

        cfg.ReceiveEndpoint(options.ImportQueueName,
            e => { e.ConfigureConsumer<ConfigImportRequestConsumer>(context); });

        cfg.ReceiveEndpoint(options.RollbackQueueName,
            e => { e.ConfigureConsumer<ConfigRollbackRequestConsumer>(context); });
    }
}
