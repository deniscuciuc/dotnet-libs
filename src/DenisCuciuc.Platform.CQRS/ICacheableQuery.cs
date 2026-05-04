namespace DenisCuciuc.Platform.CQRS;

/// <summary>
/// Preferred cache contract for query results.
/// The <see cref="CachingBehavior{TRequest,TResult}"/> pipeline behavior
/// uses <see cref="QueryCachePolicy"/> to read/write cached query responses.
/// </summary>
public interface ICacheableQuery<TResponse> : IQuery<TResponse>
{
    QueryCachePolicy CachePolicy { get; }
}
