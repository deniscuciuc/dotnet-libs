namespace DenisCuciuc.Platform.MongoDB.Migrations;

public interface IMigrationRunner
{
    Task RunAsync(MigrationVersion toVersion, CancellationToken cancellationToken);
}
