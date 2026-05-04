using DenisCuciuc.Platform.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.MongoDB.Startup;

/// <summary>
/// Connects all registered <see cref="IMongoDBConnection"/> instances in parallel.
/// </summary>
public sealed class MongoConnectStartup : IStartup
{
    public int Order => 0;

    public async Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        var connections = host.Services.GetServices<IMongoDBConnection>().ToList();

        host.Logger.LogInformation("Connecting to MongoDB ({Count} connection(s)) ...", connections.Count);

        await Task.WhenAll(connections.Select(x => x.ConnectAsync()));

        host.MarkConfigured<IMongoDBProvider>();

        host.Logger.LogInformation("MongoDB connected");
    }
}
