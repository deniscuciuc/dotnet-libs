using MassTransit;

namespace CoreLibs.MQ;

public interface IMqConsumerConfigurator
{
    IMqConsumerConfigurator AddConsumer<T>() where T : class, IConsumer;
}
