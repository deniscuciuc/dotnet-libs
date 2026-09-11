namespace CoreLibs.LiveConfig;

/// <summary>
/// Thread-safe, lock-free in-memory config cache.
/// Uses Volatile reads and Interlocked swaps for zero-allocation hot-path reads.
/// </summary>
public sealed class ConfigCache<T> : IConfigCache<T> where T : class
{
    private sealed record CacheSnapshot(T? Data, int Version);

    private CacheSnapshot _snapshot = new(null, 0);

    public T? Current => Volatile.Read(ref _snapshot).Data;

    public int CurrentVersion => Volatile.Read(ref _snapshot).Version;

    public bool IsLoaded => Volatile.Read(ref _snapshot).Data is not null;

    public T GetRequired()
    {
        var snapshot = Volatile.Read(ref _snapshot);
        return snapshot.Data
               ?? throw new InvalidOperationException(
                   $"Config cache for {typeof(T).Name} has not been loaded yet. " +
                   "Ensure the config type is listed in CriticalConfigTypes or preloaded before use.");
    }

    public void Update(T data, int version)
    {
        ArgumentNullException.ThrowIfNull(data);
        Interlocked.Exchange(ref _snapshot, new CacheSnapshot(data, version));
    }

    public void Invalidate()
    {
        Interlocked.Exchange(ref _snapshot, new CacheSnapshot(null, 0));
    }
}
