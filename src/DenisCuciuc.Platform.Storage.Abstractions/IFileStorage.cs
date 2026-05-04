namespace DenisCuciuc.Platform.Storage.Abstractions;

/// <summary>
/// Abstraction for file storage operations (S3-compatible or local disk).
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Uploads a file to the specified bucket and returns metadata about the stored object.
    /// </summary>
    Task<StorageMetadata> UploadAsync(
        string bucket,
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from storage as a stream.
    /// </summary>
    Task<Stream> DownloadAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage. Idempotent â€” does not throw if the file is missing.
    /// </summary>
    Task DeleteAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a file exists in storage.
    /// </summary>
    Task<bool> ExistsAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned URL for temporary access to a file.
    /// </summary>
    Task<string> GetPresignedUrlAsync(
        string bucket,
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves metadata about a stored file without downloading its contents.
    /// </summary>
    Task<StorageMetadata> GetMetadataAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists objects in a bucket that match the given prefix.
    /// </summary>
    Task<IReadOnlyList<StorageObjectInfo>> ListAsync(
        string bucket,
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies an object from one location to another. Supports cross-bucket copies.
    /// </summary>
    Task CopyAsync(
        string sourceBucket,
        string sourceKey,
        string destBucket,
        string destKey,
        CancellationToken cancellationToken = default);
}
