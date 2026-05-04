namespace DenisCuciuc.Platform.Cache;

/// <summary>
/// Observability hook for cache hit/miss tracking.
/// Implement against Prometheus or OpenTelemetry in the consuming service.
/// </summary>
public interface ICacheMetrics
{
    void RecordHit(string key);
    void RecordMiss(string key);
}
