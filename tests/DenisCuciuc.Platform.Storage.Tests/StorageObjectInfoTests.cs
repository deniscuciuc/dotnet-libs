using DenisCuciuc.Platform.Storage.Abstractions;

namespace DenisCuciuc.Platform.Storage.Tests;

public class StorageObjectInfoTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var info = new StorageObjectInfo("folder/file.bin", 512, now);

        Assert.Equal("folder/file.bin", info.Key);
        Assert.Equal(512, info.Size);
        Assert.Equal(now, info.LastModified);
    }
}
