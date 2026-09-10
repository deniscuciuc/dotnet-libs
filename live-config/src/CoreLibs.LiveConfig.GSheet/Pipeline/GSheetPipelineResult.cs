namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Rich result from a pipeline execution containing domain data, errors, and warnings.
/// </summary>
/// <typeparam name="TDomain">The domain model type.</typeparam>
public sealed record GSheetPipelineResult<TDomain>
{
    public required IReadOnlyList<TDomain> Data { get; init; }
    public required IReadOnlyList<GSheetValidationError> Errors { get; init; }
    public required IReadOnlyList<GSheetWarning> Warnings { get; init; }
    public required int TotalRows { get; init; }
    public required int ValidRows { get; init; }
    public required int SkippedRows { get; init; }

    public bool HasErrors => Errors.Count > 0;
    public bool IsSuccess => !HasErrors;

    public static GSheetPipelineResult<TDomain> Success(
        IReadOnlyList<TDomain> data,
        int totalRows,
        int validRows,
        IReadOnlyList<GSheetWarning>? warnings = null)
    {
        return new GSheetPipelineResult<TDomain>
        {
            Data = data,
            Errors = [],
            Warnings = warnings ?? [],
            TotalRows = totalRows,
            ValidRows = validRows,
            SkippedRows = totalRows - validRows
        };
    }

    public static GSheetPipelineResult<TDomain> WithErrors(
        IReadOnlyList<TDomain> data,
        IReadOnlyList<GSheetValidationError> errors,
        int totalRows,
        int validRows,
        IReadOnlyList<GSheetWarning>? warnings = null)
    {
        return new GSheetPipelineResult<TDomain>
        {
            Data = data,
            Errors = errors,
            Warnings = warnings ?? [],
            TotalRows = totalRows,
            ValidRows = validRows,
            SkippedRows = totalRows - validRows
        };
    }
}
