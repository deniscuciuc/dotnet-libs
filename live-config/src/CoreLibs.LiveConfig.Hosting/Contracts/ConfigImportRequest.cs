namespace CoreLibs.LiveConfig.Hosting.Contracts;

/// <summary>
/// MQ command to request a config import.
/// </summary>
public sealed record ConfigImportRequest
{
    /// <summary>
    /// Optional: specific source/importer name. If null/empty, all sources run.
    /// </summary>
    public string? SourceName { get; init; }

    /// <summary>
    /// Optional backward-compatible alias for <see cref="SourceName"/>.
    /// </summary>
    public string? ImporterName
    {
        get => SourceName;
        init => SourceName = value;
    }

    /// <summary>
    /// Optional: import only the specified config types. When provided without <see cref="SourceName"/>,
    /// the worker will resolve each config type from the first source that can produce it.
    /// </summary>
    public IReadOnlyList<string>? ConfigTypes { get; init; }

    /// <summary>
    /// Optional: who triggered the import.
    /// </summary>
    public string? RequestedBy { get; init; }
}
