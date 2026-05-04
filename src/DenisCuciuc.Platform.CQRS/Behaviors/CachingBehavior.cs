using DenisCuciuc.Platform.Cache;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DenisCuciuc.Platform.CQRS;

/// <summary>
/// MediatR pipeline behavior that caches responses for cacheable queries.
/// Uses the platform-wide <see cref="ICache"/> abstraction.
/// </summary>
public sealed class CachingBehavior<TRequest, TResult>(
    IServiceProvider serviceProvider,
    ICacheKeyBuilder keyBuilder,
    IOptionsMonitor<PlatformCqrsOptions> optionsMonitor,
    ILogger<CachingBehavior<TRequest, TResult>> logger)
    : IPipelineBehavior<TRequest, ErrorOr<TResult>>
    where TRequest : IRequest<ErrorOr<TResult>>
{
    public async Task<ErrorOr<TResult>> Handle(
        TRequest request,
        RequestHandlerDelegate<ErrorOr<TResult>> next,
        CancellationToken cancellationToken)
    {
        var options = optionsMonitor.CurrentValue;
        if (!options.EnableCachingBehavior || !options.Caching.Enabled)
            return await next();

        var cache = serviceProvider.GetService<ICache>();
        if (cache is null)
            return await next();

        var policy = ResolvePolicy(request);
        if (policy is null || policy.BypassCacheRead)
            return await next();

        var cacheKey = BuildCacheKey(keyBuilder, options.Caching.KeyPrefix, policy.Key);

        try
        {
            var cached = await cache.GetAsync<CachedResponse<TResult>>(cacheKey);
            if (cached is not null)
            {
                logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return cached.Value;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for {CacheKey}, falling through to handler", cacheKey);
        }

        var response = await next();

        if (!policy.BypassCacheWrite && (!response.IsError || options.Caching.CacheErrors))
        {
            try
            {
                var expiry = ResolveExpiry(policy, options.Caching);
                await cache.SetAsync(cacheKey, new CachedResponse<TResult>(response), expiry);
                logger.LogDebug("Cached result for {CacheKey} (TTL: {TTL})", cacheKey, expiry);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cache write failed for {CacheKey}", cacheKey);
            }
        }

        return response;
    }

    private static QueryCachePolicy? ResolvePolicy(TRequest request)
    {
        if (request is ICacheableQuery<TResult> typedCacheable)
            return typedCacheable.CachePolicy;

        return null;
    }

    private static string BuildCacheKey(ICacheKeyBuilder keyBuilder, string prefix, string key)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return key;

        return keyBuilder.Build(prefix, key);
    }

    private static TimeSpan? ResolveExpiry(QueryCachePolicy policy, PlatformCqrsCachingOptions options)
    {
        var expiry = policy.AbsoluteExpiration ?? policy.SlidingExpiration ?? options.DefaultAbsoluteExpiration;
        if (expiry is null || expiry <= TimeSpan.Zero)
            return null;

        if (options.MaxJitterSeconds <= 0)
            return expiry;

        var jitterSeconds = Random.Shared.Next(0, options.MaxJitterSeconds + 1);
        return expiry.Value.Add(TimeSpan.FromSeconds(jitterSeconds));
    }

    private sealed record CachedResponse<T>(ErrorOr<T> Value);
}
