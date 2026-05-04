using DenisCuciuc.Platform.Storage.Disk;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DenisCuciuc.Platform.Storage.Tests;

public class DiskStorageInitializerTests : IDisposable
{
    private readonly string _basePath = Path.Combine(Path.GetTempPath(), $"init-test-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
            Directory.Delete(_basePath, recursive: true);
    }

    [Fact]
    public async Task InitializeAsync_EmptyBuckets_DoesNotCreateDirectories()
    {
        var options = Options.Create(new DiskStorageOptions { BasePath = _basePath, Buckets = [] });
        var initializer = new DiskStorageInitializer(options, NullLogger<DiskStorageInitializer>.Instance);

        await initializer.InitializeAsync();

        Assert.False(Directory.Exists(_basePath));
    }

    [Fact]
    public async Task InitializeAsync_CreatesBucketDirectories()
    {
        var options = Options.Create(new DiskStorageOptions
        {
            BasePath = _basePath,
            Buckets = ["images", "documents"]
        });
        var initializer = new DiskStorageInitializer(options, NullLogger<DiskStorageInitializer>.Instance);

        await initializer.InitializeAsync();

        Assert.True(Directory.Exists(Path.Combine(_basePath, "images")));
        Assert.True(Directory.Exists(Path.Combine(_basePath, "documents")));
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent_WhenDirectoryExists()
    {
        var options = Options.Create(new DiskStorageOptions
        {
            BasePath = _basePath,
            Buckets = ["already-exists"]
        });
        Directory.CreateDirectory(Path.Combine(_basePath, "already-exists"));

        var initializer = new DiskStorageInitializer(options, NullLogger<DiskStorageInitializer>.Instance);

        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        Assert.True(Directory.Exists(Path.Combine(_basePath, "already-exists")));
    }
}
