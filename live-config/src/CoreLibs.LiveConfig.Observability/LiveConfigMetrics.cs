using System.Diagnostics.Metrics;

namespace CoreLibs.LiveConfig.Observability;

/// <summary>
/// OpenTelemetry metrics for LiveConfig operations.
/// </summary>
public sealed class LiveConfigMetrics : IDisposable
{
    private const string MeterName = "CoreLibs.LiveConfig";

    private readonly Meter _meter;

    public LiveConfigMetrics()
    {
        _meter = new Meter(MeterName);

        ImportsTotal = _meter.CreateCounter<long>(
            "liveconfig.imports.total",
            description: "Total number of config imports");

        ImportErrors = _meter.CreateCounter<long>(
            "liveconfig.imports.errors",
            description: "Total number of failed config imports");

        RollbacksTotal = _meter.CreateCounter<long>(
            "liveconfig.rollbacks.total",
            description: "Total number of config rollbacks");

        LoadsTotal = _meter.CreateCounter<long>(
            "liveconfig.loads.total",
            description: "Total number of config loads");

        ImportDuration = _meter.CreateHistogram<double>(
            "liveconfig.imports.duration",
            "ms",
            "Duration of config import operations");

        ActiveVersions = _meter.CreateObservableGauge(
            "liveconfig.active_versions",
            () => _activeVersionSnapshots,
            description: "Current active version per config type");
    }

    private Counter<long> ImportsTotal { get; }
    private Counter<long> ImportErrors { get; }
    private Counter<long> RollbacksTotal { get; }
    private Counter<long> LoadsTotal { get; }
    private Histogram<double> ImportDuration { get; }
    public ObservableGauge<int> ActiveVersions { get; }

    private IEnumerable<Measurement<int>> _activeVersionSnapshots = [];

    /// <summary>
    /// Updates the gauge snapshot of active versions.
    /// </summary>
    public void SetActiveVersions(IReadOnlyDictionary<string, int> manifest)
    {
        _activeVersionSnapshots = manifest
            .Select(kv => new Measurement<int>(kv.Value, new KeyValuePair<string, object?>("config_type", kv.Key)))
            .ToList();
    }

    public void RecordImport(string configType, bool success, double durationMs)
    {
        var tag = new KeyValuePair<string, object?>("config_type", configType);
        ImportsTotal.Add(1, tag);
        ImportDuration.Record(durationMs, tag);
        if (!success) ImportErrors.Add(1, tag);
    }

    public void RecordRollback(string configType)
    {
        RollbacksTotal.Add(1, new KeyValuePair<string, object?>("config_type", configType));
    }

    public void RecordLoad(string configType)
    {
        LoadsTotal.Add(1, new KeyValuePair<string, object?>("config_type", configType));
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
