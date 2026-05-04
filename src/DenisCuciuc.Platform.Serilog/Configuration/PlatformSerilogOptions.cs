namespace DenisCuciuc.Platform.Serilog.Configuration;

public sealed class PlatformSerilogOptions
{
    public string? ApplicationName { get; set; }
    public SerilogProfile Profile { get; set; } = SerilogProfile.Default;

    public bool EnableMachineName { get; set; } = true;
    public bool EnableEnvironment { get; set; } = true;

    /// <summary>
    /// Enriches logs with the correlation ID read from <see cref="CorrelationId.CorrelationIdOptions.HeaderName"/>.
    /// Requires <c>AddPlatformCorrelationId()</c> on <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>
    /// so that <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/> is registered.
    /// Set to <c>false</c> for Worker / Console apps that never use the correlation middleware.
    /// </summary>
    public bool EnableCorrelationId { get; set; } = true;
}
