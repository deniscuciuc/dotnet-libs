namespace DenisCuciuc.Platform.CQRS;

public sealed record QueryCachePolicy
{
    public required string Key { get; init; }

    public TimeSpan? AbsoluteExpiration { get; init; }

    public TimeSpan? SlidingExpiration { get; init; }

    public bool BypassCacheRead { get; init; }

    public bool BypassCacheWrite { get; init; }

    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();

    public static QueryCachePolicy Absolute(string key, TimeSpan expiration) => new()
    {
        Key = key,
        AbsoluteExpiration = expiration
    };

    public static QueryCachePolicy Sliding(string key, TimeSpan expiration) => new()
    {
        Key = key,
        SlidingExpiration = expiration
    };
}
