namespace CoreLibs.Cache;

public static class CacheExtensions
{
    /// <summary>
    /// Returns the cached value for <paramref name="key"/> if present; otherwise invokes
    /// <paramref name="factory"/>, stores the result, and returns it.
    /// </summary>
    public static async Task<T> GetOrSetAsync<T>(
        this ICache cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiry = null)
    {
        var existing = await cache.GetAsync<T>(key);
        if (existing is not null)
            return existing;

        var value = await factory();
        await cache.SetAsync(key, value, expiry);
        return value;
    }

    /// <summary>
    /// Returns the cached value for <paramref name="key"/> if present; otherwise invokes
    /// <paramref name="factory"/>, stores the result using <paramref name="policy"/>, and returns it.
    /// </summary>
    public static Task<T> GetOrSetAsync<T>(
        this ICache cache,
        string key,
        Func<Task<T>> factory,
        CachePolicy policy) =>
        cache.GetOrSetAsync(key, factory, policy.Expiry);

    /// <summary>Sets a value using <paramref name="policy"/> to drive expiry.</summary>
    public static Task<bool> SetAsync<T>(
        this ICache cache,
        string key,
        T value,
        CachePolicy policy,
        When when = When.Always) =>
        cache.SetAsync(key, value, policy.Expiry, when);
}
