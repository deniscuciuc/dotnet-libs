using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DenisCuciuc.Platform.Jobs.Hangfire;

public static class PlatformJobsDashboardExtensions
{
    /// <summary>
    /// Maps the Hangfire dashboard UI. Only active when <c>Jobs:Dashboard:Enabled</c> is <c>true</c>.
    /// </summary>
    public static IApplicationBuilder? MapPlatformJobsDashboard(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetRequiredService<IOptionsMonitor<PlatformJobsOptions>>().CurrentValue;

        if (!options.Dashboard.Enabled)
            return null;

        var dashboardOptions = new DashboardOptions
        {
            Authorization = [new PlatformJobsDashboardAuthFilter(options.Dashboard.AuthMode)],
            DashboardTitle = "DenisCuciuc.Platform Jobs"
        };

        return app.UseHangfireDashboard(options.Dashboard.Path, dashboardOptions);
    }
}

/// <summary>
/// Dashboard authorization filter supporting multiple auth modes.
/// </summary>
internal sealed class PlatformJobsDashboardAuthFilter(DashboardAuthMode authMode) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return authMode switch
        {
            DashboardAuthMode.AllowAll => true,
            DashboardAuthMode.AllowLocal => IsLocal(httpContext),
            DashboardAuthMode.Identity => httpContext.User.Identity?.IsAuthenticated == true,
            _ => false
        };
    }

    private static bool IsLocal(HttpContext context)
    {
        var connection = context.Connection;
        if (connection.RemoteIpAddress is null)
            return true;

        return connection.LocalIpAddress is not null
            ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
            : System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress);
    }
}
