using CoreLibs.LiveConfig.GSheet;
using CsvHelper.Configuration;

namespace CoreLibs.Localization.LiveConfig.GSheet;

/// <summary>
/// Bridges the localization GSheet parsing into the LiveConfig importer pipeline.
/// The orchestrator calls CreateMapper → ValidateRows → MapToDomain → ImportAsync.
/// </summary>
[GSheetImporter(LocalizationConstants.GSheetTabName)]
public sealed class LocalizationGSheetImporter : IGSheetImporter<LocalizationGSheetRow, LocalizationSnapshot>
{
    public ClassMap<LocalizationGSheetRow> CreateMapper()
    {
        return LocalizationGSheetMapper.Default;
    }

    public GSheetValidationResult ValidateRows(IReadOnlyList<LocalizationGSheetRow> rows)
    {
        var errors = new List<GSheetValidationError>();
        for (var i = 0; i < rows.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(rows[i].Key))
                errors.Add(new GSheetValidationError(i, "Key", "Key cannot be empty"));
            if (rows[i].Translations.Count == 0)
                errors.Add(new GSheetValidationError(i, "Translations", "Row has no translations"));
        }

        return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
    }

    public IEnumerable<LocalizationSnapshot> MapToDomain(
        IReadOnlyList<LocalizationGSheetRow> rows, GSheetImportContext context)
    {
        return LocalizationGSheetConverter.Convert(rows);
    }

    public Task<bool> ImportAsync(
        IEnumerable<LocalizationSnapshot> domainModels, GSheetImportContext context)
    {
        return Task.FromResult(true);
    }
}
