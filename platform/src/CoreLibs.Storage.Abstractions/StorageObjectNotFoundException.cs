namespace CoreLibs.Storage.Abstractions;

public sealed class StorageObjectNotFoundException(string bucket, string key, Exception? innerException = null)
    : Exception($"Storage object was not found: {bucket}/{key}", innerException)
{
    public string Bucket { get; } = bucket;

    public string Key { get; } = key;
}
