namespace CoreLibs.Serilog.RequestLogging;

public sealed class RequestLoggingConfig
{
    public string? DefaultLevel { get; set; } = "Information";
    public List<PathLevelRule> PathLevels { get; set; } = [];
    public RequestBodyOptions Body { get; set; } = new();
}
