using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Serilog.CorrelationId;

public static class CorrelationIdExtensions
{
    /// <summary>
    /// Registers <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>, which is required
    /// by <c>Serilog.Enrichers.CorrelationId</c> to read the correlation ID from the current request.
    /// Call this before <c>builder.Build()</c>.
    /// </summary>
    public static IServiceCollection AddCoreCorrelationId(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        return services;
    }

    /// <summary>
    /// Adds the correlation ID middleware. Reads <c>X-Correlation-ID</c> from the incoming request
    /// (or generates a new <see cref="Guid"/> if absent) and optionally echoes it in the response.
    /// Place this before <c>UseCoreRequestLogging()</c> and <c>UseAuthentication()</c>.
    /// </summary>
    public static IApplicationBuilder UseCoreCorrelationId(
        this IApplicationBuilder app,
        Action<CorrelationIdOptions>? configure = null)
    {
        var options = new CorrelationIdOptions();
        configure?.Invoke(options);
        return app.UseMiddleware<CorrelationIdMiddleware>(options);
    }
}
