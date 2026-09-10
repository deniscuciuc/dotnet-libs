namespace CoreLibs.LiveConfig;

/// <summary>
/// Result of a config import operation.
/// </summary>
/// <param name="ConfigType">The configuration type identifier.</param>
/// <param name="Changed">Whether the data actually changed (new version created).</param>
/// <param name="Version">The active version after the operation.</param>
/// <param name="Error">Error message if the operation failed.</param>
public sealed record ConfigImportResult(
    string ConfigType,
    bool Changed,
    int Version,
    string? Error = null)
{
    public bool IsSuccess => Error is null;
}
