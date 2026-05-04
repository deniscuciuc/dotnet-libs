using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.HealthCheck;

public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddPlatformHealthChecks(this IServiceCollection services)
    {
        return services.AddHealthChecks().AddOkHealthCheck();
    }

    public static IApplicationBuilder UsePlatformHealthChecks(this IApplicationBuilder app, string path = "/health")
    {
        return app.UseHealthChecks(path);
    }
}
