namespace CoreLibs.Storage.Abstractions;

/// <summary>
/// Summary information about a stored object, returned when listing bucket contents.
/// </summary>
/// <param name="Key">The storage key (path) of the object.</param>
/// <param name="Size">File size in bytes.</param>
/// <param name="LastModified">When the object was last modified.</param>
public record StorageObjectInfo(
    string Key,
    long Size,
    DateTimeOffset LastModified);
