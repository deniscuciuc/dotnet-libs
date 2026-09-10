namespace CoreLibs.Redis;

public interface IRedisConnection
{
    Task ConnectAsync();
}
