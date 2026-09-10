namespace CoreLibs.LiveConfig.GSheet;

/// <summary>
/// Aggregated report from a full GSheet import run.
/// Contains per-importer breakdown with rows, errors, warnings, and timing.
/// </summary>
public sealed class GSheetImportReport
{
    private readonly List<ImporterReport> _importerReports = [];

    public IReadOnlyList<ImporterReport> ImporterReports => _importerReports;
    public int TotalImporters => _importerReports.Count;
    public int TotalRows => _importerReports.Sum(r => r.TotalRows);
    public int TotalValidRows => _importerReports.Sum(r => r.ValidRows);
    public int TotalSkippedRows => _importerReports.Sum(r => r.SkippedRows);
    public int TotalErrors => _importerReports.Sum(r => r.Errors.Count);
    public int TotalWarnings => _importerReports.Sum(r => r.Warnings.Count);
    public bool HasErrors => _importerReports.Any(r => r.HasErrors);

    internal void Add(ImporterReport report)
    {
        _importerReports.Add(report);
    }

    internal void Clear()
    {
        _importerReports.Clear();
    }

    /// <summary>
    /// Per-importer breakdown.
    /// </summary>
    public sealed class ImporterReport
    {
        public required string ImporterName { get; init; }
        public required string SheetName { get; init; }
        public required string Range { get; init; }
        public required int TotalRows { get; init; }
        public required int ValidRows { get; init; }
        public int SkippedRows => TotalRows - ValidRows;
        public required IReadOnlyList<GSheetValidationError> Errors { get; init; }
        public required IReadOnlyList<GSheetWarning> Warnings { get; init; }
        public required TimeSpan Duration { get; init; }
        public required bool UsedPipelineApi { get; init; }
        public bool HasErrors => Errors.Count > 0;
    }
}
