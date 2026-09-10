namespace CoreLibs.LiveConfig.GSheet;

public sealed class GSheetValidationResult
{
    private GSheetValidationResult(bool isSuccess, List<GSheetValidationError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public IReadOnlyList<GSheetValidationError> Errors { get; }
    public bool HasErrors => Errors.Count > 0;

    public static GSheetValidationResult Ok()
    {
        return new GSheetValidationResult(true, []);
    }

    public static GSheetValidationResult Fail(params GSheetValidationError[] errors)
    {
        return new GSheetValidationResult(false, [.. errors]);
    }

    public static GSheetValidationResult Fail(IEnumerable<GSheetValidationError> errors)
    {
        return new GSheetValidationResult(false, [.. errors]);
    }

    /// <summary>
    /// Merges multiple validation results into a single result.
    /// </summary>
    public static GSheetValidationResult Merge(params GSheetValidationResult[] results)
    {
        var errors = results.SelectMany(r => r.Errors).ToList();
        return errors.Count == 0 ? Ok() : Fail(errors);
    }

    /// <summary>
    /// Merges multiple validation results into a single result.
    /// </summary>
    public static GSheetValidationResult Merge(IEnumerable<GSheetValidationResult> results)
    {
        var errors = results.SelectMany(r => r.Errors).ToList();
        return errors.Count == 0 ? Ok() : Fail(errors);
    }
}

public sealed record GSheetValidationError(
    int RowIndex,
    string Field,
    string Reason,
    string? SheetName = null,
    int? SpreadsheetRow = null,
    string? Column = null)
{
    /// <summary>
    /// Creates an error with automatic spreadsheet row calculation (row index + 2 for header offset).
    /// </summary>
    public static GSheetValidationError ForSheet(string sheetName, int rowIndex, string field, string reason)
    {
        return new GSheetValidationError(rowIndex, field, reason, sheetName, rowIndex >= 0 ? rowIndex + 2 : null,
            field);
    }

    public override string ToString()
    {
        var location = (SheetName, SpreadsheetRow) switch
        {
            (not null, not null) => $"[{SheetName}!Row {SpreadsheetRow}] ",
            (not null, null) => $"[{SheetName}] ",
            (null, _) when RowIndex >= 0 => $"Row {RowIndex + 2}: ",
            _ => ""
        };
        return $"{location}{Field} - {Reason}";
    }
}

/// <summary>
/// Represents a non-fatal issue detected during import (e.g., extra columns, default-filled cells).
/// </summary>
public sealed record GSheetWarning(string Field, string Message)
{
    public override string ToString()
    {
        return $"{Field}: {Message}";
    }
}
