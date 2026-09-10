namespace CoreLibs.LiveConfig;

/// <summary>
/// Result of a batch config import operation.
/// </summary>
public sealed class ConfigBatchImportResult
{
    /// <summary>
    /// Per-config results.
    /// </summary>
    public IReadOnlyList<ConfigImportResult> Results { get; init; } = [];

    /// <summary>
    /// The transaction mode used for this batch.
    /// </summary>
    public ImportTransactionMode Mode { get; init; }

    /// <summary>
    /// True if ALL individual results succeeded.
    /// </summary>
    public bool AllSucceeded => Results.All(r => r.IsSuccess);

    /// <summary>
    /// True if ANY individual result has an error.
    /// </summary>
    public bool HasErrors => Results.Any(r => !r.IsSuccess);

    /// <summary>
    /// True if the batch was rolled back due to transactional mode failure.
    /// </summary>
    public bool WasRolledBack { get; init; }

    /// <summary>
    /// Number of configs that were changed.
    /// </summary>
    public int ChangedCount => Results.Count(r => r.Changed);

    /// <summary>
    /// Number of configs that were unchanged (hash match).
    /// </summary>
    public int UnchangedCount => Results.Count(r => r.IsSuccess && !r.Changed);

    /// <summary>
    /// Number of configs that failed.
    /// </summary>
    public int FailedCount => Results.Count(r => !r.IsSuccess);
}
