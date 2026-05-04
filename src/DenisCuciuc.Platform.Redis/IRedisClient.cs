using StackExchange.Redis;

namespace DenisCuciuc.Platform.Redis;

public interface IRedisClient
{
    ConnectionMultiplexer Connection { get; }
    IDatabase Database { get; }
}
