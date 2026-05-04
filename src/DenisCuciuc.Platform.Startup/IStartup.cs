namespace DenisCuciuc.Platform.Startup;

public interface IStartup
{
    /// <summary>Execution order. Lower values run first. Default is <c>0</c>.</summary>
    int Order => 0;

    Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default);
}
