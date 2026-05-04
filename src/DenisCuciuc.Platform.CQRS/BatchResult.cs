using ErrorOr;

namespace DenisCuciuc.Platform.CQRS;

/// <summary>
/// Hard limits for batch operations.
/// </summary>
public static class BatchLimits
{
    /// <summary>Maximum number of items per batch request.</summary>
    public const int DefaultMaxBatchSize = 100;
}

/// <summary>
/// Result of a single item within a batch operation.
/// </summary>
public record BatchOperationResult(
    Guid ItemId,
    bool Success,
    Error? Error = null);

/// <summary>
/// Aggregate result for a batch command that returns no per-item values.
/// </summary>
public record BatchResult(IReadOnlyList<BatchOperationResult> Results)
{
    public int SuccessCount => Results.Count(r => r.Success);
    public int FailureCount => Results.Count(r => !r.Success);
    public bool AllSucceeded => Results.All(r => r.Success);
}

/// <summary>
/// Result of a single item within a batch operation that produces a new value per item.
/// </summary>
public record BatchOperationResult<TValue>(
    Guid ItemId,
    bool Success,
    TValue? Value = default,
    Error? Error = null);

/// <summary>
/// Aggregate result for a batch command that returns a value per item.
/// </summary>
public record BatchResult<TValue>(IReadOnlyList<BatchOperationResult<TValue>> Results)
{
    public int SuccessCount => Results.Count(r => r.Success);
    public int FailureCount => Results.Count(r => !r.Success);
    public bool AllSucceeded => Results.All(r => r.Success);
}
