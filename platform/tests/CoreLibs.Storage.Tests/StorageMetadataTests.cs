using CoreLibs.Storage.Abstractions;

namespace CoreLibs.Storage.Tests;

public class StorageMetadataTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var meta = new StorageMetadata("my/key.txt", 1024, "text/plain", now, "abc123");

        Assert.Equal("my/key.txt", meta.Key);
        Assert.Equal(1024, meta.Size);
        Assert.Equal("text/plain", meta.ContentType);
        Assert.Equal(now, meta.LastModified);
        Assert.Equal("abc123", meta.ETag);
    }

    [Fact]
    public void ETag_CanBeNull()
    {
        var meta = new StorageMetadata("k", 0, "application/octet-stream", DateTimeOffset.UtcNow, null);
        Assert.Null(meta.ETag);
    }
}
