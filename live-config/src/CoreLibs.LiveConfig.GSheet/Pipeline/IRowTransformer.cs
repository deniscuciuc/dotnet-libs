namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Transforms/enriches rows after parsing and validation, before domain mapping.
/// Use for: string normalization, enum resolution, computed fields, data cleanup.
/// </summary>
/// <typeparam name="TRow">The parsed row type.</typeparam>
public interface IRowTransformer<TRow>
{
    /// <summary>
    /// Transforms rows in-place or returns a new list of transformed rows.
    /// </summary>
    IReadOnlyList<TRow> Transform(IReadOnlyList<TRow> rows, GSheetImportContext context);
}
