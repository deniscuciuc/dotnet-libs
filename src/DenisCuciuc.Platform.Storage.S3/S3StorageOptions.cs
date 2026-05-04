using System.ComponentModel.DataAnnotations;

namespace DenisCuciuc.Platform.Storage.S3;

/// <summary>
/// Configuration options for S3-compatible storage (MinIO, AWS S3, etc.).
/// </summary>
public sealed class S3StorageOptions
{
    public const string DefaultSectionPath = "Storage:S3";

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; } = true;
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Bucket names to create at startup. Applications define their own bucket names.
    /// </summary>
    public string[] Buckets { get; set; } = [];
}
