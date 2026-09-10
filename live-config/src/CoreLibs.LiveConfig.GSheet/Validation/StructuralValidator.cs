using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.GSheet.Validation;

/// <summary>
/// Validates sheet headers against the expected schema derived from
/// <see cref="GSheetColumnAttribute"/> annotations on <typeparamref name="TRow"/>.
/// Runs before CsvHelper parsing to detect missing or extra columns early.
/// </summary>
public static class StructuralValidator
{
    /// <summary>
    /// Validates that the provided headers contain all expected columns.
    /// </summary>
    /// <param name="actualHeaders">The header row values from the Google Sheet.</param>
    /// <param name="warnings">Populated with non-fatal issues (extra columns).</param>
    /// <typeparam name="TRow">The row type whose attributes define the expected schema.</typeparam>
    /// <returns>Validation result — fails if required columns are missing.</returns>
    public static GSheetValidationResult Validate<TRow>(
        IReadOnlyList<string> actualHeaders,
        out IReadOnlyList<GSheetWarning> warnings)
    {
        return Validate(typeof(TRow), actualHeaders, out warnings);
    }

    /// <summary>
    /// Validates that the provided headers contain all expected columns.
    /// </summary>
    public static GSheetValidationResult Validate(
        Type rowType,
        IReadOnlyList<string> actualHeaders,
        out IReadOnlyList<GSheetWarning> warnings)
    {
        var expectedColumns = AttributeClassMapBuilder.GetExpectedColumns(rowType);
        var actualNormalized = new HashSet<string>(
            actualHeaders.Select(h => ColumnNameNormalizer.Normalize(h)));

        // Check for missing columns (normalized comparison)
        var errors = (from expected in expectedColumns
                      where !actualNormalized.Contains(ColumnNameNormalizer.Normalize(expected))
                      select new GSheetValidationError(-1, expected,
                          $"Expected column '{expected}' is missing from the sheet headers")).ToList();

        // Check for extra columns (non-fatal → warning)
        var expectedNormalized = new HashSet<string>(
            expectedColumns.Select(ColumnNameNormalizer.Normalize));
        var warningList = (from actual in actualHeaders
                           select actual.Trim()
            into trimmed
                           where !string.IsNullOrEmpty(trimmed) &&
                                 !expectedNormalized.Contains(ColumnNameNormalizer.Normalize(trimmed))
                           select new GSheetWarning(trimmed,
                               $"Unexpected column '{trimmed}' found in sheet headers (will be ignored)")).ToList();

        warnings = warningList;
        return errors.Count == 0
            ? GSheetValidationResult.Ok()
            : GSheetValidationResult.Fail(errors);
    }
}
