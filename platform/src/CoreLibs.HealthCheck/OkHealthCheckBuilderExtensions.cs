using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.HealthCheck;

public static class OkHealthCheckBuilderExtensions
{
    public static IHealthChecksBuilder AddOkHealthCheck(this IHealthChecksBuilder builder, string name = "ok")
    {
        // Tag as liveness so it can be filtered later if readiness checks are added.
        return builder.AddCheck<OkHealthCheck>(name, tags: ["liveness"]);
    }
}
