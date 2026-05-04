namespace DenisCuciuc.Platform.Storage.Disk;

/// <summary>
/// Configuration options for local disk storage.
/// </summary>
public sealed class DiskStorageOptions
{
    public const string DefaultSectionPath = "Storage:Disk";

    /// <summary>
    /// Base directory for local file storage.
    /// </summary>
    public string BasePath { get; set; } = Path.Combine(Path.GetTempPath(), "deniscuciuc-platform-storage");

    /// <summary>
    /// Bucket directories to create at startup. Applications define their own bucket names.
    /// </summary>
    public string[] Buckets { get; set; } = [];
}
