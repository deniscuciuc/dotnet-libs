using DenisCuciuc.Platform.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.MongoDB.Startup;

/// <summary>
/// Applies repository indexes in parallel for all <see cref="IRepositoryApplyIndex"/> registrations.
/// </summary>
public sealed class MongoIndexStartup : IStartup
{
    public int Order => 1;

    public async Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        var repositories = host.Services.GetServices<IRepositoryApplyIndex>().ToList();

        host.Logger.LogInformation("Applying MongoDB indexes ({Count} repository/ies) ...", repositories.Count);

        await Task.WhenAll(repositories.Select(x =>
        {
            var loggerType = typeof(ILogger<>).MakeGenericType(x.GetType());
            var logger = (ILogger)host.Services.GetRequiredService(loggerType);
            return x.ApplyAsync(logger);
        }));

        host.Logger.LogInformation("MongoDB indexes applied");
    }
}
