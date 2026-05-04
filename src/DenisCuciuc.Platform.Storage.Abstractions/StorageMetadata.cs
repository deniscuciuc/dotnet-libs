namespace DenisCuciuc.Platform.Storage.Abstractions;

/// <summary>
/// Metadata about a stored file, returned by upload and metadata retrieval operations.
/// </summary>
/// <param name="Key">The storage key (path) of the object.</param>
/// <param name="Size">File size in bytes.</param>
/// <param name="ContentType">MIME content type of the file.</param>
/// <param name="LastModified">When the object was last modified.</param>
/// <param name="ETag">Entity tag (usually an MD5 hash) for cache validation. May be null for disk storage.</param>
public record StorageMetadata(
    string Key,
    long Size,
    string ContentType,
    DateTimeOffset LastModified,
    string? ETag);
