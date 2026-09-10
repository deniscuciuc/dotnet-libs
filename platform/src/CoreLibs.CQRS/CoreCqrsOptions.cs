using System.ComponentModel.DataAnnotations;

namespace CoreLibs.CQRS;

public sealed class CoreCqrsOptions
{
    public const string DefaultSectionPath = "Cqrs";

    public bool EnableLoggingBehavior { get; set; } = true;

    public bool EnableValidationBehavior { get; set; } = true;

    public bool EnableCachingBehavior { get; set; } = true;

    public bool EnableDetailedErrorLogging { get; set; }

    [Range(1, int.MaxValue)]
    public int DefaultBatchMaxSize { get; set; } = BatchLimits.DefaultMaxBatchSize;

    public CoreCqrsCachingOptions Caching { get; set; } = new();
}

public sealed class CoreCqrsCachingOptions
{
    public bool Enabled { get; set; } = true;

    public string KeyPrefix { get; set; } = string.Empty;

    [Range(0, 3600)]
    public int MaxJitterSeconds { get; set; }

    public TimeSpan? DefaultAbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(5);

    public bool CacheErrors { get; set; }
}
