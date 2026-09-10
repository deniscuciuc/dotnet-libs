using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreLibs.HealthCheck;

public sealed class OkHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Always return Healthy. Message kept short for minimal overhead.
        return Task.FromResult(HealthCheckResult.Healthy("ok"));
    }
}
