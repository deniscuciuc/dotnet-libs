namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Cross-row and cross-sheet validation. Runs after row-level validation.
/// Use for: duplicate ID detection, missing references, circular dependencies, business invariants.
/// </summary>
/// <typeparam name="TRow">The parsed row type.</typeparam>
public interface IGraphValidator<in TRow>
{
    /// <summary>
    /// Validates rows in the context of the full import graph.
    /// </summary>
    GSheetValidationResult Validate(IReadOnlyList<TRow> rows, GSheetImportContext context);
}
