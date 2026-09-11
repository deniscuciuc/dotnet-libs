using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace CoreLibs.LiveConfig.Hosting;

/// <summary>
/// Maps direct (synchronous) LiveConfig HTTP endpoints for import, rollback, and manifest.
/// </summary>
public static class LiveConfigEndpointExtensions
{
    /// <summary>
    /// Maps direct LiveConfig management endpoints:
    /// <list type="bullet">
    /// <item><c>POST /configs/import</c> — batch import from all sources (mode from options or ?mode= override)</item>
    /// <item><c>POST /configs/import/{sourceName}</c> — import from a specific source</item>
    /// <item><c>POST /configs/{configType}/rollback/{version}</c> — rollback to a specific version</item>
    /// <item><c>GET  /configs/manifest</c> — current version manifest</item>
    /// </list>
    /// </summary>
    public static IEndpointRouteBuilder MapLiveConfigEndpoints(
        this IEndpointRouteBuilder app,
        string prefix = "/configs")
    {
        var group = app.MapGroup(prefix).WithTags("Live Config");

        group.MapPost("/import", async (
            HttpContext httpContext,
            LiveConfigEngine engine,
            ConfigSourceRegistry sourceRegistry,
            IOptions<LiveConfigOptions> options) =>
        {
            var mode = ResolveImportMode(httpContext, options.Value);
            var results = new List<ConfigImportResult>();

            foreach (var source in sourceRegistry.All)
            {
                var snapshots = await source.FetchAllAsync(httpContext.RequestAborted);
                var batch = await engine.ImportBatchAsync(snapshots, mode, "api", httpContext.RequestAborted);
                results.AddRange(batch.Results);
            }

            return Results.Ok(new ConfigBatchImportResult
            {
                Results = results,
                Mode = mode
            });
        });

        group.MapPost("/import/{sourceName}", async (
            string sourceName,
            HttpContext httpContext,
            LiveConfigEngine engine,
            ConfigSourceRegistry sourceRegistry,
            IOptions<LiveConfigOptions> options) =>
        {
            var source = sourceRegistry.Resolve(sourceName);
            if (source is null)
                return Results.NotFound(new { error = $"Source '{sourceName}' not found." });

            var mode = ResolveImportMode(httpContext, options.Value);
            var snapshots = await source.FetchAllAsync(httpContext.RequestAborted);
            var result = await engine.ImportBatchAsync(snapshots, mode, "api", httpContext.RequestAborted);

            return result.WasRolledBack
                ? Results.UnprocessableEntity(result)
                : Results.Ok(result);
        });

        group.MapPost("/{configType}/rollback/{version:int}", async (
            string configType,
            int version,
            LiveConfigEngine engine,
            CancellationToken ct) =>
        {
            var result = await engine.RollbackAsync(configType, version, "api", ct);
            return result.Changed
                ? Results.Ok(result)
                : Results.BadRequest(result);
        });

        group.MapGet("/manifest", async (LiveConfigEngine engine, CancellationToken ct) =>
            Results.Ok(await engine.GetManifestAsync(ct)));

        return app;
    }

    private static ImportTransactionMode ResolveImportMode(HttpContext httpContext, LiveConfigOptions options)
    {
        var query = httpContext.Request.Query["mode"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(query) &&
            Enum.TryParse<ImportTransactionMode>(query, ignoreCase: true, out var overrideMode))
            return overrideMode;

        return options.DefaultImportMode;
    }
}
