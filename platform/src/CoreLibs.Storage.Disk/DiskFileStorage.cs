using System.Text.Json;
using CoreLibs.Storage.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.Storage.Disk;

/// <summary>
/// Local disk <see cref="IFileStorage"/> implementation.
/// Stores files on the local filesystem using bucket/key as directory/file paths.
/// Content type is persisted in a <c>.meta</c> JSON sidecar file alongside each object.
/// </summary>
public sealed class DiskFileStorage(
    IOptions<DiskStorageOptions> options,
    ILogger<DiskFileStorage> logger) : IFileStorage
{
    private string BasePath => options.Value.BasePath;

    public async Task<StorageMetadata> UploadAsync(
        string bucket, string key, Stream content, string contentType,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Uploading {Key} to disk bucket {Bucket}", key, bucket);

        var filePath = GetFilePath(bucket, key);
        var directory = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(directory);

        await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        await WriteMetaFileAsync(filePath, contentType, cancellationToken);

        var fileInfo = new FileInfo(filePath);

        logger.LogInformation("Uploaded {Key} to disk bucket {Bucket} ({Size} bytes)", key, bucket, fileInfo.Length);

        return new StorageMetadata(
            Key: key,
            Size: fileInfo.Length,
            ContentType: contentType,
            LastModified: new DateTimeOffset(fileInfo.LastWriteTimeUtc),
            ETag: null);
    }

    public Task<Stream> DownloadAsync(
        string bucket, string key,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Downloading {Key} from disk bucket {Bucket}", key, bucket);

        var filePath = GetFilePath(bucket, key);

        if (!File.Exists(filePath))
        {
            throw new StorageObjectNotFoundException(bucket, key);
        }

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string bucket, string key,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Deleting {Key} from disk bucket {Bucket}", key, bucket);

        var filePath = GetFilePath(bucket, key);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        var metaPath = filePath + ".meta";
        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }

        logger.LogInformation("Deleted {Key} from disk bucket {Bucket}", key, bucket);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        string bucket, string key,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(bucket, key);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<string> GetPresignedUrlAsync(
        string bucket, string key, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(bucket, key);

        if (!File.Exists(filePath))
        {
            throw new StorageObjectNotFoundException(bucket, key);
        }

        var uri = new Uri(filePath).AbsoluteUri;
        return Task.FromResult(uri);
    }

    public async Task<StorageMetadata> GetMetadataAsync(
        string bucket, string key,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting metadata for {Key} in disk bucket {Bucket}", key, bucket);

        var filePath = GetFilePath(bucket, key);

        if (!File.Exists(filePath))
        {
            throw new StorageObjectNotFoundException(bucket, key);
        }

        var fileInfo = new FileInfo(filePath);
        var contentType = await ReadContentTypeAsync(filePath, cancellationToken);

        return new StorageMetadata(
            Key: key,
            Size: fileInfo.Length,
            ContentType: contentType,
            LastModified: new DateTimeOffset(fileInfo.LastWriteTimeUtc),
            ETag: null);
    }

    public Task<IReadOnlyList<StorageObjectInfo>> ListAsync(
        string bucket, string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Listing objects in disk bucket {Bucket} with prefix {Prefix}", bucket, prefix ?? "(none)");

        var bucketPath = Path.Combine(BasePath, bucket);

        if (!Directory.Exists(bucketPath))
        {
            return Task.FromResult<IReadOnlyList<StorageObjectInfo>>([]);
        }

        var searchPath = prefix is not null
            ? Path.Combine(bucketPath, prefix.Replace('/', Path.DirectorySeparatorChar))
            : bucketPath;

        var searchDir = Directory.Exists(searchPath) ? searchPath : Path.GetDirectoryName(searchPath) ?? bucketPath;
        var pattern = Directory.Exists(searchPath) ? "*" : Path.GetFileName(searchPath) + "*";

        var files = Directory.GetFiles(searchDir, pattern, SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));

        var results = (from file in files
                       let relativePath = Path.GetRelativePath(bucketPath, file).Replace(Path.DirectorySeparatorChar, '/')
                       let info = new FileInfo(file)
                       select new StorageObjectInfo(Key: relativePath, Size: info.Length, LastModified: new DateTimeOffset(info.LastWriteTimeUtc)))
            .ToList();

        logger.LogDebug("Listed {Count} objects in disk bucket {Bucket}", results.Count, bucket);

        return Task.FromResult<IReadOnlyList<StorageObjectInfo>>(results);
    }

    public async Task CopyAsync(
        string sourceBucket, string sourceKey,
        string destBucket, string destKey,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Copying {SourceKey} from {SourceBucket} to {DestKey} in {DestBucket}",
            sourceKey, sourceBucket, destKey, destBucket);

        var sourcePath = GetFilePath(sourceBucket, sourceKey);

        if (!File.Exists(sourcePath))
        {
            throw new StorageObjectNotFoundException(sourceBucket, sourceKey);
        }

        var destPath = GetFilePath(destBucket, destKey);
        var destDir = Path.GetDirectoryName(destPath)!;
        Directory.CreateDirectory(destDir);

        File.Copy(sourcePath, destPath, overwrite: true);

        var sourceMetaPath = sourcePath + ".meta";
        if (File.Exists(sourceMetaPath))
        {
            File.Copy(sourceMetaPath, destPath + ".meta", overwrite: true);
        }

        logger.LogInformation("Copied {SourceBucket}/{SourceKey} â†’ {DestBucket}/{DestKey}",
            sourceBucket, sourceKey, destBucket, destKey);

        await Task.CompletedTask;
    }

    private string GetFilePath(string bucket, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var bucketRoot = Path.GetFullPath(Path.Combine(BasePath, bucket));
        var safePath = key.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(bucketRoot, safePath));

        if (!fullPath.StartsWith(bucketRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Invalid storage key path: {key}");

        return fullPath;
    }

    private static async Task WriteMetaFileAsync(string filePath, string contentType,
        CancellationToken cancellationToken)
    {
        var metaPath = filePath + ".meta";
        var meta = new DiskObjectMeta { ContentType = contentType };
        var json = JsonSerializer.Serialize(meta);
        await File.WriteAllTextAsync(metaPath, json, cancellationToken);
    }

    private static async Task<string> ReadContentTypeAsync(string filePath, CancellationToken cancellationToken)
    {
        var metaPath = filePath + ".meta";

        if (!File.Exists(metaPath))
        {
            return "application/octet-stream";
        }

        var json = await File.ReadAllTextAsync(metaPath, cancellationToken);
        var meta = JsonSerializer.Deserialize<DiskObjectMeta>(json);
        return meta?.ContentType ?? "application/octet-stream";
    }

    private sealed class DiskObjectMeta
    {
        public string ContentType { get; init; } = "application/octet-stream";
    }
}
