namespace CoreLibs.LiveConfig.Hosting.Contracts;

/// <summary>
/// MQ response after rollback completes.
/// </summary>
public sealed record ConfigRollbackCompleted
{
    public required string ConfigType { get; init; }
    public int Version { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? RequestedBy { get; init; }
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}
