using System.Diagnostics;

namespace CoreLibs.LiveConfig.Observability;

/// <summary>
/// OpenTelemetry ActivitySource for distributed tracing of LiveConfig operations.
/// </summary>
public static class LiveConfigActivitySource
{
    private const string Name = "CoreLibs.LiveConfig";

    private static readonly ActivitySource Source = new(Name);

    public static Activity? StartImport(string configType)
    {
        return Source.StartActivity($"liveconfig.import {configType}",
            ActivityKind.Internal,
            parentContext: default,
            tags: [new KeyValuePair<string, object?>("liveconfig.config_type", configType)]);
    }

    public static Activity? StartLoad(string configType)
    {
        return Source.StartActivity($"liveconfig.load {configType}",
            ActivityKind.Internal,
            parentContext: default,
            tags: [new KeyValuePair<string, object?>("liveconfig.config_type", configType)]);
    }

    public static Activity? StartRollback(string configType)
    {
        return Source.StartActivity($"liveconfig.rollback {configType}",
            ActivityKind.Internal,
            parentContext: default,
            tags: [new KeyValuePair<string, object?>("liveconfig.config_type", configType)]);
    }

    public static Activity? StartFetch(string sourceName, string configType)
    {
        return Source.StartActivity($"liveconfig.fetch {sourceName}/{configType}",
            ActivityKind.Client,
            parentContext: default,
            tags:
            [
                new KeyValuePair<string, object?>("liveconfig.source", sourceName),
                new KeyValuePair<string, object?>("liveconfig.config_type", configType)
            ]);
    }
}
