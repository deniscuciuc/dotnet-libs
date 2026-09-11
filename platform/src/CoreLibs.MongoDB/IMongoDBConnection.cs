namespace CoreLibs.MongoDB;

public interface IMongoDBConnection
{
    Task ConnectAsync();
}
