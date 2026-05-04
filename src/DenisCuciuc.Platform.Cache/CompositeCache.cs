namespace DenisCuciuc.Platform.Cache;

/// <summary>
/// Two-level cache that reads from a fast primary (e.g. in-memory) before falling through
/// to a slower secondary (e.g. Redis). On a secondary hit the value is back-filled into
/// the primary for subsequent reads.
/// </summary>
public sealed class CompositeCache : ICache
{
    private readonly ICache _primary;
    private readonly ICache _secondary;
    private readonly TimeSpan? _primaryExpiry;

    /// <param name="primary">Fast layer, e.g. an in-memory cache.</param>
    /// <param name="secondary">Distributed layer, e.g. Redis.</param>
    /// <param name="primaryExpiry">TTL used when back-filling the primary on a secondary hit.</param>
    public CompositeCache(ICache primary, ICache secondary, TimeSpan? primaryExpiry = null)
    {
        _primary = primary;
        _secondary = secondary;
        _primaryExpiry = primaryExpiry;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _primary.GetAsync<T>(key);
        if (value is not null)
            return value;

        value = await _secondary.GetAsync<T>(key);
        if (value is not null)
            await _primary.SetAsync(key, value, _primaryExpiry);

        return value;
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always)
    {
        var primaryResult = await _primary.SetAsync(key, value, _primaryExpiry ?? expiry, when);
        var secondaryResult = await _secondary.SetAsync(key, value, expiry, when);
        return primaryResult && secondaryResult;
    }

    public async Task<bool> DeleteAsync(string key)
    {
        var primaryResult = await _primary.DeleteAsync(key);
        var secondaryResult = await _secondary.DeleteAsync(key);
        return primaryResult || secondaryResult;
    }
}
