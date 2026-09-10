namespace CoreLibs.LiveConfig.Hosting.Contracts;

/// <summary>
/// MQ command to request a config rollback.
/// </summary>
public sealed record ConfigRollbackRequest
{
    public required string ConfigType { get; init; }
    public int? TargetVersion { get; init; }
    public string? RequestedBy { get; init; }
}
