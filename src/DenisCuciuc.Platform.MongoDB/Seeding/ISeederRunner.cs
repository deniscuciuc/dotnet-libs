namespace DenisCuciuc.Platform.MongoDB.Seeding;

public interface ISeederRunner
{
    Task RunAsync(CancellationToken cancellationToken);
}
