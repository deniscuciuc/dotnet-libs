using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace CoreLibs.Serilog.RequestLogging;

public static class RequestLoggingApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Serilog request logging with path-level rules and optional body enrichment.
    /// Reads configuration from <c>Serilog:RequestLogging</c> in <c>appsettings.json</c>.
    /// Place after <c>UseCoreCorrelationId()</c> and before <c>UseRouting()</c>.
    /// </summary>
    public static IApplicationBuilder UseCoreRequestLogging(this IApplicationBuilder app)
    {
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var cfg = configuration.GetSection("Serilog:RequestLogging").Get<RequestLoggingConfig>()
                  ?? new RequestLoggingConfig();

        var parsedRules = cfg.PathLevels
            .Where(r => !string.IsNullOrWhiteSpace(r.Path))
            .Select(r => new ParsedRule(
                r.Path!,
                r.PrefixMatch,
                Enum.TryParse<LogEventLevel>(r.Level, true, out var lvl) ? lvl : LogEventLevel.Information))
            .ToList();

        var defaultLevel = Enum.TryParse<LogEventLevel>(cfg.DefaultLevel, true, out var d)
            ? d
            : LogEventLevel.Information;

        app.UseMiddleware<SerilogMiddleware>(cfg.Body);

        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, _, _) =>
            {
                var path = httpContext.Request.Path;
                foreach (var rule in parsedRules)
                {
                    if (rule.PrefixMatch
                            ? path.StartsWithSegments(rule.Path, StringComparison.OrdinalIgnoreCase)
                            : path.Equals(rule.Path, StringComparison.OrdinalIgnoreCase))
                        return rule.Level;
                }
                return defaultLevel;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("ClientIP",
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            };
        });

        return app;
    }

    private sealed record ParsedRule(string Path, bool PrefixMatch, LogEventLevel Level);
}
