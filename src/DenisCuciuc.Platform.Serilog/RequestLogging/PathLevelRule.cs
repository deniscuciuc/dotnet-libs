namespace DenisCuciuc.Platform.Serilog.RequestLogging;

public sealed class PathLevelRule
{
    public string? Path { get; set; }
    public string? Level { get; set; }
    public bool PrefixMatch { get; set; } = true;
}
