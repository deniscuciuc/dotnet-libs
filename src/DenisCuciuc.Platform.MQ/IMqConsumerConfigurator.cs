using MassTransit;

namespace DenisCuciuc.Platform.MQ;

public interface IMqConsumerConfigurator
{
    IMqConsumerConfigurator AddConsumer<T>() where T : class, IConsumer;
}
