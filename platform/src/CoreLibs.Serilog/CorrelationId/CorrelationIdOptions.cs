namespace CoreLibs.Serilog.CorrelationId;

public sealed class CorrelationIdOptions
{
    public string HeaderName { get; set; } = "X-Correlation-ID";
    public bool IncludeInResponse { get; set; } = true;
}
