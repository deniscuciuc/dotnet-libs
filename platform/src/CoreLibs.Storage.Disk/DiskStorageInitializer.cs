using CoreLibs.Storage.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.Storage.Disk;

/// <summary>
/// Creates bucket directories on the local filesystem at application startup.
/// Bucket names are configured via <see cref="DiskStorageOptions.Buckets"/>.
/// </summary>
public sealed class DiskStorageInitializer(
    IOptions<DiskStorageOptions> options,
    ILogger<DiskStorageInitializer> logger) : IStorageInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var basePath = options.Value.BasePath;
        var buckets = options.Value.Buckets;

        if (buckets.Length == 0)
        {
            logger.LogDebug("No disk storage buckets configured for initialization");
            return Task.CompletedTask;
        }

        logger.LogInformation("Initializing disk storage directories at {BasePath}", basePath);

        foreach (var bucket in buckets)
        {
            var bucketPath = Path.Combine(basePath, bucket);

            if (!Directory.Exists(bucketPath))
            {
                Directory.CreateDirectory(bucketPath);
                logger.LogInformation("Created disk bucket directory {BucketPath}", bucketPath);
            }
            else
            {
                logger.LogDebug("Disk bucket directory {BucketPath} already exists", bucketPath);
            }
        }

        logger.LogInformation("Disk storage initialization complete");
        return Task.CompletedTask;
    }
}
