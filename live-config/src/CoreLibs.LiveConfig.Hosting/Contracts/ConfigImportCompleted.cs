namespace CoreLibs.LiveConfig.Hosting.Contracts;

/// <summary>
/// MQ response after import completes.
/// </summary>
public sealed record ConfigImportCompleted
{
    public required IReadOnlyList<ConfigImportResult> Results { get; init; }
    public string? RequestedBy { get; init; }
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}
