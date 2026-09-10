using CoreLibs.Cache;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.CQRS;

/// <summary>
/// MediatR pipeline behavior that invalidates configured cache keys
/// for successful requests implementing <see cref="IInvalidatesCache"/>.
/// </summary>
public sealed class CacheInvalidationBehavior<TRequest, TResult>(
    IServiceProvider serviceProvider,
    ICacheKeyBuilder keyBuilder,
    IOptionsMonitor<CoreCqrsOptions> optionsMonitor,
    ILogger<CacheInvalidationBehavior<TRequest, TResult>> logger)
    : IPipelineBehavior<TRequest, ErrorOr<TResult>>
    where TRequest : IRequest<ErrorOr<TResult>>
{
    public async Task<ErrorOr<TResult>> Handle(
        TRequest request,
        RequestHandlerDelegate<ErrorOr<TResult>> next,
        CancellationToken cancellationToken)
    {
        var response = await next();
        if (response.IsError || request is not IInvalidatesCache invalidatesCache)
            return response;

        var options = optionsMonitor.CurrentValue;
        if (!options.Caching.Enabled)
            return response;

        var cache = serviceProvider.GetService<ICache>();
        if (cache is null)
            return response;

        foreach (var key in invalidatesCache.CacheKeys.Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var cacheKey = BuildCacheKey(keyBuilder, options.Caching.KeyPrefix, key);

            try
            {
                await cache.DeleteAsync(cacheKey);
                logger.LogDebug("Invalidated cache key {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cache invalidation failed for {CacheKey}", cacheKey);
            }
        }

        return response;
    }

    private static string BuildCacheKey(ICacheKeyBuilder keyBuilder, string prefix, string key)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return key;

        return keyBuilder.Build(prefix, key);
    }
}
