namespace CoreLibs.LiveConfig.GSheet;

/// <summary>
/// Controls how validation errors are handled during GSheet import.
/// </summary>
public enum GSheetImportMode
{
    /// <summary>
    /// Fail the entire import if any validation error occurs.
    /// </summary>
    Strict,

    /// <summary>
    /// Skip invalid rows and continue importing valid ones (default).
    /// Matches the existing behavior prior to the pipeline redesign.
    /// </summary>
    SkipInvalid,

    /// <summary>
    /// Import all valid rows and return a full report with errors and warnings.
    /// Does not throw on validation errors.
    /// </summary>
    CollectAndReport
}
