using CoreLibs.LiveConfig.Hosting.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig.Hosting.Consumers;

/// <summary>
/// MassTransit consumer that handles config import requests.
/// Runs all or specific importers via the LiveConfigEngine, collecting results.
/// </summary>
public sealed class ConfigImportRequestConsumer(
    LiveConfigEngine engine,
    ConfigSourceRegistry sourceRegistry,
    ILogger<ConfigImportRequestConsumer> logger) : IConsumer<ConfigImportRequest>
{
    public async Task Consume(ConsumeContext<ConfigImportRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;
        var sourceName = request.SourceName ?? request.ImporterName;
        var requestedConfigTypes = request.ConfigTypes?
            .Where(configType => !string.IsNullOrWhiteSpace(configType))
            .Select(configType => configType.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        logger.LogInformation(
            "Received config import request. Source: {Source}, ConfigTypes: {ConfigTypes}, RequestedBy: {RequestedBy}",
            sourceName ?? "(all)",
            requestedConfigTypes.Count == 0 ? "(all)" : string.Join(", ", requestedConfigTypes),
            request.RequestedBy);

        var manifestBefore = await engine.GetManifestAsync(ct);

        IReadOnlyList<ConfigImportResult> results;

        if (requestedConfigTypes.Count > 0)
        {
            results = await ImportSpecificConfigTypesAsync(sourceName, requestedConfigTypes, request.RequestedBy, ct);
        }
        else if (!string.IsNullOrWhiteSpace(sourceName))
        {
            var source = sourceRegistry.Resolve(sourceName);
            if (source is null)
            {
                logger.LogWarning("Source '{Source}' not found", sourceName);
                results =
                [
                    new ConfigImportResult(sourceName, false, 0, $"Source '{sourceName}' not found")
                ];
            }
            else
            {
                results = await ImportSourceAsync(source, request.RequestedBy, ct);
            }
        }
        else
        {
            results = await ImportAllSourcesAsync(request.RequestedBy, ct);
        }

        var completed = new ConfigImportCompleted
        {
            Results = results,
            RequestedBy = request.RequestedBy,
            CompletedAt = DateTimeOffset.UtcNow
        };

        await context.Publish(completed, ct);

        var manifestAfter = await engine.GetManifestAsync(ct);
        LogManifestDiff(manifestBefore, manifestAfter);
    }

    private async Task<IReadOnlyList<ConfigImportResult>> ImportSpecificConfigTypesAsync(
        string? sourceName,
        IReadOnlyList<string> configTypes,
        string? requestedBy,
        CancellationToken ct)
    {
        IConfigSource? source = null;
        if (!string.IsNullOrWhiteSpace(sourceName))
        {
            source = sourceRegistry.Resolve(sourceName);
            if (source is null)
            {
                logger.LogWarning("Source '{Source}' not found for targeted import", sourceName);
                return [new ConfigImportResult(sourceName, false, 0, $"Source '{sourceName}' not found")];
            }
        }

        var snapshots = new Dictionary<string, ConfigSnapshot>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<ConfigImportResult>();

        foreach (var configType in configTypes)
        {
            try
            {
                var snapshot = source is not null
                    ? await source.FetchOrDefaultAsync(configType, ct)
                    : await FetchFromAnySourceAsync(configType, ct);

                if (snapshot is null)
                {
                    failures.Add(new ConfigImportResult(
                        configType,
                        false,
                        0,
                        source is null
                            ? $"No source could produce config type '{configType}'"
                            : $"Source '{source.SourceName}' could not produce config type '{configType}'"));
                    continue;
                }

                snapshots[configType] = snapshot;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error importing config type {ConfigType} from {Source}",
                    configType, source?.SourceName ?? "(auto)");
                failures.Add(new ConfigImportResult(configType, false, 0, ex.Message));
            }
        }

        if (snapshots.Count == 0)
            return failures;

        var batch = await engine.ImportBatchAsync(snapshots, createdBy: requestedBy, cancellationToken: ct);
        return batch.Results.Concat(failures).ToList();
    }

    private async Task<ConfigSnapshot?> FetchFromAnySourceAsync(string configType, CancellationToken ct)
    {
        foreach (var source in sourceRegistry.All)
        {
            var snapshot = await source.FetchOrDefaultAsync(configType, ct);
            if (snapshot is not null)
                return snapshot;
        }

        return null;
    }

    private async Task<IReadOnlyList<ConfigImportResult>> ImportSourceAsync(
        IConfigSource source,
        string? requestedBy,
        CancellationToken ct)
    {
        var snapshots = await source.FetchAllAsync(ct);
        var batch = await engine.ImportBatchAsync(snapshots, createdBy: requestedBy, cancellationToken: ct);
        return batch.Results;
    }

    private async Task<IReadOnlyList<ConfigImportResult>> ImportAllSourcesAsync(string? requestedBy,
        CancellationToken ct)
    {
        var results = new List<ConfigImportResult>();

        foreach (var source in sourceRegistry.All)
            try
            {
                results.AddRange(await ImportSourceAsync(source, requestedBy, ct));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error importing from source {Source}", source.SourceName);
                results.Add(new ConfigImportResult(source.SourceName, false, 0, ex.Message));
            }

        return results;
    }

    private void LogManifestDiff(
        IReadOnlyDictionary<string, int> before,
        IReadOnlyDictionary<string, int> after)
    {
        foreach (var (key, version) in after)
            if (!before.TryGetValue(key, out var prevVersion) || prevVersion != version)
                logger.LogInformation("Config {ConfigType}: v{Before} → v{After}",
                    key, before.GetValueOrDefault(key, 0), version);
    }
}
