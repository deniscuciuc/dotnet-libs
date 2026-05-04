using System.Diagnostics;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;

namespace DenisCuciuc.Platform.Jobs.Hangfire.Filters;

/// <summary>
/// Propagates and restores correlation ID across job boundaries.
/// Stores the correlation ID from the current activity/HTTP context into job parameters,
/// and restores it when the job is executed.
/// </summary>
internal sealed class PlatformJobCorrelationFilter : JobFilterAttribute, IClientFilter, IServerFilter
{
    private const string CorrelationIdKey = "Platform-CorrelationId";
    private const string TraceIdKey = "Platform-TraceId";

    public void OnCreating(CreatingContext context)
    {
        // Capture correlation context at enqueue time
        var correlationId = Activity.Current?.TraceId.ToString()
                            ?? Guid.NewGuid().ToString("N");

        context.SetJobParameter(CorrelationIdKey, correlationId);

        if (Activity.Current is not null)
            context.SetJobParameter(TraceIdKey, Activity.Current.TraceId.ToString());
    }

    public void OnCreated(CreatedContext context)
    {
    }

    public void OnPerforming(PerformingContext context)
    {
        // Restore correlation context at execution time
        var correlationId = context.GetJobParameter<string>(CorrelationIdKey);

        if (!string.IsNullOrEmpty(correlationId))
        {
            // Set on the current activity for downstream observability
            Activity.Current?.SetTag("platform.correlation_id", correlationId);
            Activity.Current?.SetBaggage("correlation-id", correlationId);
        }
    }

    public void OnPerformed(PerformedContext context)
    {
    }
}
