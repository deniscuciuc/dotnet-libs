namespace CoreLibs.CQRS;

/// <summary>
/// Marker contract for requests that should invalidate one or more cache keys after successful execution.
/// Keys should be provided without CQRS prefixing; pipeline behavior applies configured key prefixing.
/// </summary>
public interface IInvalidatesCache
{
    IEnumerable<string> CacheKeys { get; }
}
