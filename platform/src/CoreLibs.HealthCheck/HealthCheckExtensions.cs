using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.HealthCheck;

public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddCoreHealthChecks(this IServiceCollection services)
    {
        return services.AddHealthChecks().AddOkHealthCheck();
    }

    public static IApplicationBuilder UseCoreHealthChecks(this IApplicationBuilder app, string path = "/health")
    {
        return app.UseHealthChecks(path);
    }
}
