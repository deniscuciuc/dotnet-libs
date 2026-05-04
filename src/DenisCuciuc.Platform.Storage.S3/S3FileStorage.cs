using Amazon.S3;
using Amazon.S3.Model;
using DenisCuciuc.Platform.Storage.Abstractions;
using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.Storage.S3;

/// <summary>
/// S3-compatible <see cref="IFileStorage"/> implementation using AWS SDK.
/// Works with MinIO, AWS S3, and other S3-compatible services.
/// </summary>
public sealed class S3FileStorage(
    IAmazonS3 s3Client,
    ILogger<S3FileStorage> logger) : IFileStorage
{
    public async Task<StorageMetadata> UploadAsync(
        string bucket, string key, Stream content, string contentType,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Uploading {Key} to bucket {Bucket}", key, bucket);

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        };

        await s3Client.PutObjectAsync(request, cancellationToken);

        var meta = await GetMetadataAsync(bucket, key, cancellationToken);

        logger.LogInformation("Uploaded {Key} to bucket {Bucket} ({Size} bytes)", key, bucket, meta.Size);

        return meta;
    }

    public async Task<Stream> DownloadAsync(
        string bucket, string key,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Downloading {Key} from bucket {Bucket}", key, bucket);

        var request = new GetObjectRequest
        {
            BucketName = bucket,
            Key = key,
        };

        GetObjectResponse response;
        try
        {
            response = await s3Client.GetObjectAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new StorageObjectNotFoundException(bucket, key, ex);
        }

        var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(
        string bucket, string key,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Deleting {Key} from bucket {Bucket}", key, bucket);

        var request = new DeleteObjectRequest
        {
            BucketName = bucket,
            Key = key,
        };

        await s3Client.DeleteObjectAsync(request, cancellationToken);

        logger.LogInformation("Deleted {Key} from bucket {Bucket}", key, bucket);
    }

    public async Task<bool> ExistsAsync(
        string bucket, string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucket,
                Key = key,
            };

            await s3Client.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public Task<string> GetPresignedUrlAsync(
        string bucket, string key, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Generating presigned URL for {Key} in bucket {Bucket} (expiry: {Expiry})", key, bucket, expiry);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET,
        };

        var url = s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public async Task<StorageMetadata> GetMetadataAsync(
        string bucket, string key,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting metadata for {Key} in bucket {Bucket}", key, bucket);

        var request = new GetObjectMetadataRequest
        {
            BucketName = bucket,
            Key = key,
        };

        GetObjectMetadataResponse response;
        try
        {
            response = await s3Client.GetObjectMetadataAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new StorageObjectNotFoundException(bucket, key, ex);
        }

        return new StorageMetadata(
            Key: key,
            Size: response.ContentLength,
            ContentType: response.Headers.ContentType,
            LastModified: ToUtcOffset(response.LastModified),
            ETag: response.ETag?.Trim('"'));
    }

    public async Task<IReadOnlyList<StorageObjectInfo>> ListAsync(
        string bucket, string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Listing objects in bucket {Bucket} with prefix {Prefix}", bucket, prefix ?? "(none)");

        var results = new List<StorageObjectInfo>();
        string? continuationToken = null;

        do
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = prefix,
                ContinuationToken = continuationToken,
            };

            var response = await s3Client.ListObjectsV2Async(request, cancellationToken);

            foreach (var obj in response.S3Objects)
            {
                results.Add(new StorageObjectInfo(
                    Key: obj.Key,
                    Size: obj.Size ?? 0,
                    LastModified: ToUtcOffset(obj.LastModified)));
            }

            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        } while (continuationToken is not null);

        logger.LogDebug("Listed {Count} objects in bucket {Bucket}", results.Count, bucket);

        return results;
    }

    public async Task CopyAsync(
        string sourceBucket, string sourceKey,
        string destBucket, string destKey,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Copying {SourceKey} from {SourceBucket} to {DestKey} in {DestBucket}",
            sourceKey, sourceBucket, destKey, destBucket);

        var request = new CopyObjectRequest
        {
            SourceBucket = sourceBucket,
            SourceKey = sourceKey,
            DestinationBucket = destBucket,
            DestinationKey = destKey,
        };

        await s3Client.CopyObjectAsync(request, cancellationToken);

        logger.LogInformation("Copied {SourceBucket}/{SourceKey} â†’ {DestBucket}/{DestKey}",
            sourceBucket, sourceKey, destBucket, destKey);
    }

    private static DateTimeOffset ToUtcOffset(DateTime? value)
    {
        if (value is null)
            return DateTimeOffset.UtcNow;

        return new DateTimeOffset(value.Value.ToUniversalTime());
    }
}
