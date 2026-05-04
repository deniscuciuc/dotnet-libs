using DenisCuciuc.Platform.Storage.Disk;

namespace DenisCuciuc.Platform.Storage.Tests;

public class DiskStorageOptionsTests
{
    [Fact]
    public void DefaultSectionPath_Is_Correct()
    {
        Assert.Equal("Storage:Disk", DiskStorageOptions.DefaultSectionPath);
    }

    [Fact]
    public void DefaultBasePath_IsInTempDir()
    {
        var options = new DiskStorageOptions();
        Assert.StartsWith(Path.GetTempPath(), options.BasePath);
    }

    [Fact]
    public void DefaultBuckets_IsEmpty()
    {
        var options = new DiskStorageOptions();
        Assert.Empty(options.Buckets);
    }
}
