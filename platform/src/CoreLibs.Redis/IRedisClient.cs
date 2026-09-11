using StackExchange.Redis;

namespace CoreLibs.Redis;

public interface IRedisClient
{
    ConnectionMultiplexer Connection { get; }
    IDatabase Database { get; }
}
