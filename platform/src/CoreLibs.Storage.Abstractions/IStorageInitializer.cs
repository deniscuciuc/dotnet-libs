namespace CoreLibs.Storage.Abstractions;

public interface IStorageInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
