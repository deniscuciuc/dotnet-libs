namespace CoreLibs.MongoDB.Seeding;

public interface ISeederRunner
{
    Task RunAsync(CancellationToken cancellationToken);
}
