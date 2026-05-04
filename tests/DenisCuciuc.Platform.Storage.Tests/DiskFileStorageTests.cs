using DenisCuciuc.Platform.Storage.Abstractions;
using DenisCuciuc.Platform.Storage.Disk;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DenisCuciuc.Platform.Storage.Tests;

public class DiskFileStorageTests : IDisposable
{
    private readonly string _basePath = Path.Combine(Path.GetTempPath(), $"disk-storage-{Guid.NewGuid():N}");
    private readonly DiskFileStorage _storage;

    public DiskFileStorageTests()
    {
        var options = Options.Create(new DiskStorageOptions { BasePath = _basePath });
        _storage = new DiskFileStorage(options, NullLogger<DiskFileStorage>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
            Directory.Delete(_basePath, recursive: true);
    }

    private static Stream ToStream(string content)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(content);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private static async Task<string> ReadAllAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    // ── Upload ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_ReturnsMetadataWithCorrectKey()
    {
        using var content = ToStream("hello world");
        var meta = await _storage.UploadAsync("bucket", "file.txt", content, "text/plain");

        Assert.Equal("file.txt", meta.Key);
    }

    [Fact]
    public async Task UploadAsync_ReturnsCorrectContentType()
    {
        using var content = ToStream("data");
        var meta = await _storage.UploadAsync("bucket", "data.json", content, "application/json");

        Assert.Equal("application/json", meta.ContentType);
    }

    [Fact]
    public async Task UploadAsync_ReturnsCorrectFileSize()
    {
        var payload = "hello world";
        using var content = ToStream(payload);
        var meta = await _storage.UploadAsync("bucket", "f.txt", content, "text/plain");

        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(payload), meta.Size);
    }

    [Fact]
    public async Task UploadAsync_SetsLastModified()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        using var content = ToStream("x");
        var meta = await _storage.UploadAsync("bucket", "ts.txt", content, "text/plain");

        Assert.True(meta.LastModified >= before);
    }

    [Fact]
    public async Task UploadAsync_CreatesNestedDirectories()
    {
        using var content = ToStream("nested");
        await _storage.UploadAsync("bucket", "a/b/c/file.txt", content, "text/plain");

        var path = Path.Combine(_basePath, "bucket", "a", "b", "c", "file.txt");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task UploadAsync_OverwritesExistingFile()
    {
        using var first = ToStream("original");
        await _storage.UploadAsync("bucket", "overwrite.txt", first, "text/plain");

        using var second = ToStream("updated");
        await _storage.UploadAsync("bucket", "overwrite.txt", second, "text/plain");

        await using var dl = await _storage.DownloadAsync("bucket", "overwrite.txt");
        var text = await ReadAllAsync(dl);
        Assert.Equal("updated", text);
    }

    // ── Download ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DownloadAsync_ReturnsCorrectContent()
    {
        using var upload = ToStream("download me");
        await _storage.UploadAsync("bucket", "dl.txt", upload, "text/plain");

        await using var stream = await _storage.DownloadAsync("bucket", "dl.txt");
        var content = await ReadAllAsync(stream);

        Assert.Equal("download me", content);
    }

    [Fact]
    public async Task DownloadAsync_ThrowsStorageObjectNotFoundException_WhenMissing()
    {
        var ex = await Assert.ThrowsAsync<StorageObjectNotFoundException>(
            () => _storage.DownloadAsync("bucket", "nonexistent.txt"));

        Assert.Equal("bucket", ex.Bucket);
        Assert.Equal("nonexistent.txt", ex.Key);
    }

    // ── Delete ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesFile()
    {
        using var content = ToStream("to delete");
        await _storage.UploadAsync("bucket", "delete-me.txt", content, "text/plain");

        await _storage.DeleteAsync("bucket", "delete-me.txt");

        Assert.False(await _storage.ExistsAsync("bucket", "delete-me.txt"));
    }

    [Fact]
    public async Task DeleteAsync_IsIdempotent_WhenFileMissing()
    {
        await _storage.DeleteAsync("bucket", "does-not-exist.txt");
    }

    [Fact]
    public async Task DeleteAsync_RemovesMetaSidecar()
    {
        using var content = ToStream("meta test");
        await _storage.UploadAsync("bucket", "with-meta.txt", content, "text/plain");

        var metaPath = Path.Combine(_basePath, "bucket", "with-meta.txt.meta");
        Assert.True(File.Exists(metaPath));

        await _storage.DeleteAsync("bucket", "with-meta.txt");

        Assert.False(File.Exists(metaPath));
    }

    // ── Exists ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenMissing()
    {
        var exists = await _storage.ExistsAsync("bucket", "ghost.txt");
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_AfterUpload()
    {
        using var content = ToStream("present");
        await _storage.UploadAsync("bucket", "present.txt", content, "text/plain");

        var exists = await _storage.ExistsAsync("bucket", "present.txt");
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_AfterDelete()
    {
        using var content = ToStream("gone");
        await _storage.UploadAsync("bucket", "gone.txt", content, "text/plain");
        await _storage.DeleteAsync("bucket", "gone.txt");

        var exists = await _storage.ExistsAsync("bucket", "gone.txt");
        Assert.False(exists);
    }

    // ── GetMetadata ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetMetadataAsync_ReturnsCorrectContentType()
    {
        using var content = ToStream("{}");
        await _storage.UploadAsync("bucket", "meta.json", content, "application/json");

        var meta = await _storage.GetMetadataAsync("bucket", "meta.json");

        Assert.Equal("application/json", meta.ContentType);
    }

    [Fact]
    public async Task GetMetadataAsync_ReturnsCorrectSize()
    {
        var payload = "hello";
        using var content = ToStream(payload);
        await _storage.UploadAsync("bucket", "size.txt", content, "text/plain");

        var meta = await _storage.GetMetadataAsync("bucket", "size.txt");

        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(payload), meta.Size);
    }

    [Fact]
    public async Task GetMetadataAsync_ThrowsStorageObjectNotFoundException_WhenMissing()
    {
        var ex = await Assert.ThrowsAsync<StorageObjectNotFoundException>(
            () => _storage.GetMetadataAsync("bucket", "nope.txt"));

        Assert.Equal("bucket", ex.Bucket);
        Assert.Equal("nope.txt", ex.Key);
    }

    // ── GetPresignedUrl ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPresignedUrlAsync_ReturnsFileUri_WhenExists()
    {
        using var content = ToStream("presign");
        await _storage.UploadAsync("bucket", "presign.txt", content, "text/plain");

        var url = await _storage.GetPresignedUrlAsync("bucket", "presign.txt", TimeSpan.FromMinutes(5));

        Assert.NotNull(url);
        Assert.StartsWith("file:///", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPresignedUrlAsync_ThrowsStorageObjectNotFoundException_WhenMissing()
    {
        var ex = await Assert.ThrowsAsync<StorageObjectNotFoundException>(
            () => _storage.GetPresignedUrlAsync("bucket", "missing.txt", TimeSpan.FromMinutes(5)));

        Assert.Equal("bucket", ex.Bucket);
        Assert.Equal("missing.txt", ex.Key);
    }

    // ── List ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_ReturnsEmpty_ForNonExistentBucket()
    {
        var items = await _storage.ListAsync("empty-bucket");
        Assert.Empty(items);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllFiles_WhenNoPrefixGiven()
    {
        using var c1 = ToStream("a");
        using var c2 = ToStream("b");
        using var c3 = ToStream("c");
        await _storage.UploadAsync("list-bucket", "file1.txt", c1, "text/plain");
        await _storage.UploadAsync("list-bucket", "file2.txt", c2, "text/plain");
        await _storage.UploadAsync("list-bucket", "file3.txt", c3, "text/plain");

        var items = await _storage.ListAsync("list-bucket");

        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task ListAsync_ExcludesMetaSidecarFiles()
    {
        using var c1 = ToStream("data");
        await _storage.UploadAsync("meta-bucket", "data.bin", c1, "application/octet-stream");

        var items = await _storage.ListAsync("meta-bucket");

        Assert.Single(items);
        Assert.DoesNotContain(items, i => i.Key.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ListAsync_FiltersWithPrefix()
    {
        using var i1 = ToStream("1");
        using var i2 = ToStream("2");
        using var i3 = ToStream("3");
        await _storage.UploadAsync("prefix-bucket", "images/a.png", i1, "image/png");
        await _storage.UploadAsync("prefix-bucket", "images/b.png", i2, "image/png");
        await _storage.UploadAsync("prefix-bucket", "docs/c.pdf", i3, "application/pdf");

        var images = await _storage.ListAsync("prefix-bucket", "images");

        Assert.Equal(2, images.Count);
        Assert.All(images, item => Assert.StartsWith("images/", item.Key));
    }

    [Fact]
    public async Task ListAsync_ReturnsCorrectFileInfo()
    {
        var payload = "12345";
        using var content = ToStream(payload);
        await _storage.UploadAsync("info-bucket", "info.txt", content, "text/plain");

        var items = await _storage.ListAsync("info-bucket");

        Assert.Single(items);
        var item = items[0];
        Assert.Equal("info.txt", item.Key);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(payload), item.Size);
    }

    // ── Copy ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CopyAsync_CopiesContentToDestination()
    {
        using var content = ToStream("copy me");
        await _storage.UploadAsync("src-bucket", "src.txt", content, "text/plain");

        await _storage.CopyAsync("src-bucket", "src.txt", "dst-bucket", "dst.txt");

        await using var dl = await _storage.DownloadAsync("dst-bucket", "dst.txt");
        var text = await ReadAllAsync(dl);
        Assert.Equal("copy me", text);
    }

    [Fact]
    public async Task CopyAsync_PreservesSourceFile()
    {
        using var content = ToStream("preserve");
        await _storage.UploadAsync("src-bucket", "preserve.txt", content, "text/plain");

        await _storage.CopyAsync("src-bucket", "preserve.txt", "dst-bucket", "dst.txt");

        Assert.True(await _storage.ExistsAsync("src-bucket", "preserve.txt"));
    }

    [Fact]
    public async Task CopyAsync_SupportsCrossBucketCopy()
    {
        using var content = ToStream("cross-bucket");
        await _storage.UploadAsync("bucket-a", "file.txt", content, "text/plain");

        await _storage.CopyAsync("bucket-a", "file.txt", "bucket-b", "copy.txt");

        Assert.True(await _storage.ExistsAsync("bucket-b", "copy.txt"));
    }

    [Fact]
    public async Task CopyAsync_ThrowsStorageObjectNotFoundException_WhenSourceMissing()
    {
        var ex = await Assert.ThrowsAsync<StorageObjectNotFoundException>(
            () => _storage.CopyAsync("src-bucket", "missing.txt", "dst-bucket", "dst.txt"));

        Assert.Equal("src-bucket", ex.Bucket);
        Assert.Equal("missing.txt", ex.Key);
    }
}
