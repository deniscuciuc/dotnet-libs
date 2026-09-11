# CoreLibs.Storage

Provider-agnostic file storage with S3 and local disk implementations. The active backend is chosen from typed configuration, and provider packages are optional.

## Setup

```csharp
// Required facade
// - CoreLibs.Storage

// Choose at least one provider package:
// - CoreLibs.Storage.S3
// - CoreLibs.Storage.Disk

// Optional startup integration:
// - CoreLibs.Storage.Startup

services.AddCoreFileStorage(configuration);

// Optional startup guard + initializer orchestration
services.AddCoreStorageStartup();
```

**appsettings.yml:**

```yaml
storage:
  Provider: S3   # S3 or Disk
  s3:
    Endpoint: "https://s3.amazonaws.com"
    Region: "us-east-1"
    AccessKey: "..."
    SecretKey: "..."
    ForcePathStyle: false
    Buckets:
      - avatars
      - documents
  disk:
    BasePath: "/var/data/uploads"
    Buckets:
      - avatars
      - documents
```

---

## IFileStorage interface

```csharp
public interface IFileStorage
{
    Task<StorageMetadata> UploadAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct = default);
    Task DeleteAsync(string bucket, string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string bucket, string key, CancellationToken ct = default);
    Task<string> GetPresignedUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default);
    Task<StorageMetadata> GetMetadataAsync(string bucket, string key, CancellationToken ct = default);
    Task<IReadOnlyList<StorageObjectInfo>> ListAsync(string bucket, string? prefix = null, CancellationToken ct = default);
    Task CopyAsync(string sourceBucket, string sourceKey, string destBucket, string destKey, CancellationToken ct = default);
}
```

---

## Example

```csharp
public class AvatarService(IFileStorage storage)
{
  private const string Bucket = "avatars";

  public async Task<StorageMetadata> UploadAvatarAsync(
        string userId, Stream image, string contentType)
    {
        var key = $"avatars/{userId}/profile.jpg";
    return await storage.UploadAsync(Bucket, key, image, contentType);
    }

    public Task<Stream> GetAvatarAsync(string userId)
    => storage.DownloadAsync(Bucket, $"avatars/{userId}/profile.jpg");
}
```

---

## Startup integration

`CoreLibs.Storage.Startup` integrates storage into `RunWithStartupAsync()`:

- Resolves `IFileStorage` at startup (fail-fast DI validation)
- Runs all registered `IStorageInitializer` implementations
- Marks `StartupGuard<IFileStorage>` configured

```csharp
await app.RunWithStartupAsync();
```

---

## Provider packages

| Package | Provider |
|---|---|
| `CoreLibs.Storage.S3` | Amazon S3 / S3-compatible (MinIO, Cloudflare R2) |
| `CoreLibs.Storage.Disk` | Local filesystem |
| `CoreLibs.Storage` | Facade — auto-selects based on `Storage:Provider` config |
| `CoreLibs.Storage.Startup` | Optional startup orchestration + `IFileStorage` guard |
| `CoreLibs.Storage.Abstractions` | `IFileStorage` interface only |
