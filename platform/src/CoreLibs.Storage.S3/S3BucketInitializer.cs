using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using CoreLibs.Storage.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.Storage.S3;

/// <summary>
/// Creates S3 buckets at application startup if they do not already exist.
/// Bucket names are configured via <see cref="S3StorageOptions.Buckets"/>.
/// </summary>
public sealed class S3BucketInitializer(
    IAmazonS3 s3Client,
    IOptions<S3StorageOptions> options,
    ILogger<S3BucketInitializer> logger) : IStorageInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var buckets = options.Value.Buckets;

        if (buckets.Length == 0)
        {
            logger.LogDebug("No S3 buckets configured for initialization");
            return;
        }

        logger.LogInformation("Initializing S3 bucketsâ€¦");

        foreach (var bucket in buckets)
        {
            try
            {
                var exists = await BucketExistsAsync(bucket, cancellationToken);

                if (!exists)
                {
                    await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucket }, cancellationToken);
                    logger.LogInformation("Created S3 bucket {Bucket}", bucket);
                }
                else
                {
                    logger.LogDebug("S3 bucket {Bucket} already exists", bucket);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize S3 bucket {Bucket}", bucket);
                throw;
            }
        }

        logger.LogInformation("S3 bucket initialization complete");
    }

    private async Task<bool> BucketExistsAsync(string bucket, CancellationToken cancellationToken)
    {
        try
        {
            await s3Client.GetBucketLocationAsync(
                new GetBucketLocationRequest { BucketName = bucket },
                cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
