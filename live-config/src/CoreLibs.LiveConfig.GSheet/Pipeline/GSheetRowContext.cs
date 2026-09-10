namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Metadata for a single parsed row, providing sheet-level context for error messages.
/// </summary>
public sealed record GSheetRowContext(
    string SheetName,
    int SheetRowNumber,
    IReadOnlyDictionary<string, string> RawValues);
