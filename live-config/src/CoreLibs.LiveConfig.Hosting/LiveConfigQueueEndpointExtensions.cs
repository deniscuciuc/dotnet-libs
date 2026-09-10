using CoreLibs.LiveConfig.Hosting.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CoreLibs.LiveConfig.Hosting;

/// <summary>
/// Maps queue-based (async) LiveConfig HTTP endpoints that publish MQ commands.
/// </summary>
public static class LiveConfigQueueEndpointExtensions
{
    /// <summary>
    /// Maps queue-based LiveConfig endpoints that publish import/rollback commands to MQ:
    /// <list type="bullet">
    /// <item><c>POST /queue/import</c> — queue import for all sources</item>
    /// <item><c>POST /queue/import/{sourceName}</c> — queue import for a specific source/importer</item>
    /// <item><c>POST /queue/import/type/{configType}</c> — queue import for a specific config type</item>
    /// <item><c>POST /queue/rollback/{configType}/{version}</c> — queue a rollback</item>
    /// </list>
    /// </summary>
    public static IEndpointRouteBuilder MapLiveConfigQueueEndpoints(
        this IEndpointRouteBuilder app,
        string prefix = "/configs/queue")
    {
        var group = app.MapGroup(prefix).WithTags("Live Config");

        group.MapPost("/import", async (IPublishEndpoint bus, CancellationToken ct) =>
        {
            await bus.Publish(new ConfigImportRequest { RequestedBy = "queue-api" }, ct);
            return Results.Accepted(value: new { message = "Import request queued (all sources)." });
        });

        group.MapPost("/import/{importerName}", async (
            string importerName,
            IPublishEndpoint bus,
            CancellationToken ct) =>
        {
            await bus.Publish(new ConfigImportRequest
            {
                SourceName = importerName,
                RequestedBy = "queue-api"
            }, ct);
            return Results.Accepted(value: new { message = $"Import request queued for source '{importerName}'." });
        });

        group.MapPost("/import/type/{configType}", async (
            string configType,
            IPublishEndpoint bus,
            CancellationToken ct) =>
        {
            await bus.Publish(new ConfigImportRequest
            {
                ConfigTypes = [configType],
                RequestedBy = "queue-api"
            }, ct);
            return Results.Accepted(value: new { message = $"Import request queued for config type '{configType}'." });
        });

        group.MapPost("/rollback/{configType}/{version:int}", async (
            string configType,
            int version,
            IPublishEndpoint bus,
            CancellationToken ct) =>
        {
            await bus.Publish(new ConfigRollbackRequest
            {
                ConfigType = configType,
                TargetVersion = version,
                RequestedBy = "queue-api"
            }, ct);
            return Results.Accepted(value: new
            {
                message = $"Rollback request queued for '{configType}' → v{version}."
            });
        });

        return app;
    }
}
