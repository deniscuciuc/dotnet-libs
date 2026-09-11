namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Custom row-level validation rules. Implement this interface to add
/// domain-specific validation beyond what attribute-based validation provides.
/// </summary>
/// <typeparam name="TRow">The parsed row type.</typeparam>
public interface IRowValidator<in TRow>
{
    /// <summary>
    /// Validates parsed rows and returns errors for invalid ones.
    /// </summary>
    GSheetValidationResult Validate(IReadOnlyList<TRow> rows, GSheetImportContext context);
}
