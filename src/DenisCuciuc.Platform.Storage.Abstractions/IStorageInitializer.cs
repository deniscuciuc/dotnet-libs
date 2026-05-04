namespace DenisCuciuc.Platform.Storage.Abstractions;

public interface IStorageInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
