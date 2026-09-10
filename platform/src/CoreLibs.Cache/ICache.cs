namespace CoreLibs.Cache;

public interface ICache
{
    Task<T?> GetAsync<T>(string key);

    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always);

    Task<bool> DeleteAsync(string key);
}
