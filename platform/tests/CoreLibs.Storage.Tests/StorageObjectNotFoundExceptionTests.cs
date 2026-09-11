using CoreLibs.Storage.Abstractions;

namespace CoreLibs.Storage.Tests;

public class StorageObjectNotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsBucketAndKey()
    {
        var ex = new StorageObjectNotFoundException("my-bucket", "path/to/file.txt");

        Assert.Equal("my-bucket", ex.Bucket);
        Assert.Equal("path/to/file.txt", ex.Key);
    }

    [Fact]
    public void Message_ContainsBucketAndKey()
    {
        var ex = new StorageObjectNotFoundException("my-bucket", "path/to/file.txt");

        Assert.Contains("my-bucket", ex.Message);
        Assert.Contains("path/to/file.txt", ex.Message);
    }

    [Fact]
    public void Constructor_AcceptsInnerException()
    {
        var inner = new IOException("disk error");
        var ex = new StorageObjectNotFoundException("b", "k", inner);

        Assert.Same(inner, ex.InnerException);
    }
}
