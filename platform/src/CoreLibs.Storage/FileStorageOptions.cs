using System.ComponentModel.DataAnnotations;

namespace CoreLibs.Storage;

public enum StorageProviderKind
{
    S3,
    Disk
}

/// <summary>
/// Configuration options for file storage.
/// </summary>
public sealed class FileStorageOptions
{
    public const string DefaultSectionPath = "Storage";

    /// <summary>
    /// The storage provider to use.
    /// </summary>
    [Required]
    public StorageProviderKind Provider { get; set; } = StorageProviderKind.S3;
}
