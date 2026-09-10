namespace CoreLibs.Cache;

public sealed class CachePolicy
{
    public static readonly CachePolicy NoExpiry = new();

    public TimeSpan? Expiry { get; init; }
    public bool SlidingExpiration { get; init; }

    public static CachePolicy Expire(TimeSpan expiry) =>
        new() { Expiry = expiry };

    public static CachePolicy Sliding(TimeSpan expiry) =>
        new() { Expiry = expiry, SlidingExpiration = true };
}
