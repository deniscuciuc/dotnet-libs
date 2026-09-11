using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoreLibs.Jobs.Hangfire;

public static class CoreJobsDashboardExtensions
{
    /// <summary>
    /// Maps the Hangfire dashboard UI. Only active when <c>Jobs:Dashboard:Enabled</c> is <c>true</c>.
    /// </summary>
    public static IApplicationBuilder? MapCoreJobsDashboard(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetRequiredService<IOptionsMonitor<CoreJobsOptions>>().CurrentValue;

        if (!options.Dashboard.Enabled)
            return null;

        var dashboardOptions = new DashboardOptions
        {
            Authorization = [new CoreJobsDashboardAuthFilter(options.Dashboard.AuthMode)],
            DashboardTitle = "CoreLibs Jobs"
        };

        return app.UseHangfireDashboard(options.Dashboard.Path, dashboardOptions);
    }
}

/// <summary>
/// Dashboard authorization filter supporting multiple auth modes.
/// </summary>
internal sealed class CoreJobsDashboardAuthFilter(DashboardAuthMode authMode) : IDashboardAuthorizationFilter
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
