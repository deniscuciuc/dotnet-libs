namespace CoreLibs.LiveConfig.GSheet;

public enum GSheetImportFailureStage
{
    Validation,
    Processing
}

public sealed record GSheetImportTarget(string SheetName, string Range)
{
    public override string ToString()
    {
        return $"{SheetName}!{Range}";
    }
}

public sealed class GSheetPreloadException(IReadOnlyList<GSheetImportTarget> targets, Exception innerException)
    : Exception($"Failed to preload {targets.Count} Google Sheet range(s).", innerException)
{
    public IReadOnlyList<GSheetImportTarget> Targets { get; } = targets;
}

public sealed class GSheetImportException : Exception
{
    private GSheetImportException(
        string message,
        string importerName,
        string sheetName,
        string range,
        GSheetImportFailureStage stage,
        IReadOnlyList<GSheetValidationError> validationErrors,
        Exception? innerException)
        : base(message, innerException)
    {
        ImporterName = importerName;
        SheetName = sheetName;
        Range = range;
        Stage = stage;
        ValidationErrors = validationErrors;
    }

    public string ImporterName { get; }
    public string SheetName { get; }
    public string Range { get; }
    public GSheetImportFailureStage Stage { get; }
    public IReadOnlyList<GSheetValidationError> ValidationErrors { get; }

    public static GSheetImportException ValidationFailure(
        string importerName,
        string sheetName,
        string range,
        IReadOnlyList<GSheetValidationError> validationErrors)
    {
        return new GSheetImportException(
            $"Validation failed for importer {importerName} with {validationErrors.Count} error(s).",
            importerName,
            sheetName,
            range,
            GSheetImportFailureStage.Validation,
            validationErrors,
            null);
    }

    public static GSheetImportException ProcessingFailure(
        string importerName,
        string sheetName,
        string range,
        Exception innerException)
    {
        return new GSheetImportException(
            $"Processing failed for importer {importerName}.",
            importerName,
            sheetName,
            range,
            GSheetImportFailureStage.Processing,
            [],
            innerException);
    }
}

public sealed class GSheetImportBatchException(IReadOnlyList<Exception> failures)
    : Exception($"Batch import failed with {failures.Count} failure(s).")
{
    public IReadOnlyList<Exception> Failures { get; } = failures;
}
