# DenisCuciuc.Platform.Storage

Provider-agnostic file storage with S3 and local disk implementations. The active backend is chosen from typed configuration, and provider packages are optional.

## Setup

```csharp
// Required facade
// - DenisCuciuc.Platform.Storage

// Choose at least one provider package:
// - DenisCuciuc.Platform.Storage.S3
// - DenisCuciuc.Platform.Storage.Disk

// Optional startup integration:
// - DenisCuciuc.Platform.Storage.Startup

services.AddPlatformFileStorage(configuration);

// Optional startup guard + initializer orchestration
services.AddPlatformStorageStartup();
```

**appsettings.yml:**

```yaml
Storage:
  Provider: S3   # S3 or Disk
  S3:
    Endpoint: "https://s3.amazonaws.com"
    Region: "us-east-1"
    AccessKey: "..."
    SecretKey: "..."
    ForcePathStyle: false
    Buckets:
      - avatars
      - documents
  Disk:
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

`DenisCuciuc.Platform.Storage.Startup` integrates storage into `RunWithStartupAsync()`:

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
| `DenisCuciuc.Platform.Storage.S3` | Amazon S3 / S3-compatible (MinIO, Cloudflare R2) |
| `DenisCuciuc.Platform.Storage.Disk` | Local filesystem |
| `DenisCuciuc.Platform.Storage` | Facade - auto-selects based on `Storage:Provider` config |
| `DenisCuciuc.Platform.Storage.Startup` | Optional startup orchestration + `IFileStorage` guard |
| `DenisCuciuc.Platform.Storage.Abstractions` | `IFileStorage` interface only |
