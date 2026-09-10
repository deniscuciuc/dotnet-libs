using CsvHelper.Configuration;

namespace CoreLibs.LiveConfig.GSheet;

/// <summary>
///     Defines the contract for importing GSheet data into domain models.
///     Flow: CreateMapper → ParseAsync → ValidateRows → MapToDomain → ImportAsync.
/// </summary>
/// <typeparam name="TRow">The GSheet row model type used by CsvHelper.</typeparam>
/// <typeparam name="TDomain">The domain model type.</typeparam>
public interface IGSheetImporter<TRow, TDomain>
{
    /// <summary>
    ///     Creates the CsvHelper ClassMap for the GSheet rows.
    /// </summary>
    ClassMap<TRow> CreateMapper();

    /// <summary>
    ///     Validates the fully parsed GSheet set of rows.
    /// </summary>
    GSheetValidationResult ValidateRows(IReadOnlyList<TRow> rows);

    /// <summary>
    ///     Maps the GSheet rows to domain models.
    /// </summary>
    IEnumerable<TDomain> MapToDomain(IReadOnlyList<TRow> rows, GSheetImportContext context);

    /// <summary>
    ///     Performs the import of the domain models into the target system.
    /// </summary>
    Task<bool> ImportAsync(IEnumerable<TDomain> domainModels, GSheetImportContext context);
}
