namespace CoreLibs.LiveConfig;

/// <summary>
/// Thread-safe in-memory cache for a specific config type.
/// </summary>
public interface IConfigCache<T> where T : class
{
    /// <summary>
    /// The current cached value, or null if not yet loaded.
    /// </summary>
    T? Current { get; }

    /// <summary>
    /// The version of the currently cached value.
    /// </summary>
    int CurrentVersion { get; }

    /// <summary>
    /// Whether the cache has been loaded with a value.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the current cached value, throwing if not yet loaded.
    /// Use this in hot paths where the config must be available.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the config has not been loaded yet.</exception>
    T GetRequired();

    /// <summary>
    /// Updates the cached value atomically.
    /// </summary>
    void Update(T data, int version);

    /// <summary>
    /// Invalidates the cache, resetting to null/0.
    /// </summary>
    void Invalidate();
}
