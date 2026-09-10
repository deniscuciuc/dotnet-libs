using CoreLibs.LiveConfig.Examples.Worker.Domain;
using CoreLibs.LiveConfig.GSheet;
using CsvHelper.Configuration;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.Legacy;

/// <summary>
/// Example GSheet row DTO for bonuses sheet.
/// Columns: Name | Amount | Currency | MinLevel | IsActive
/// </summary>
public sealed class BonusRow
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int MinLevel { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Example GSheet importer for bonuses.
/// Reads from 'Bonuses' sheet, parses rows, validates, and maps to domain.
///
/// To use: register with <c>AddGSheetImportersInAssemblyOf&lt;BonusesImporter&gt;()</c>
/// and configure GSheet options with your spreadsheet ID.
/// </summary>
[GSheetImporter("Bonuses", "A1:E")]
public class BonusesImporter : IGSheetImporter<BonusRow, BonusConfig>
{
    public ClassMap<BonusRow> CreateMapper()
    {
        return new BonusRowMap();
    }

    public GSheetValidationResult ValidateRows(IReadOnlyList<BonusRow> rows)
    {
        var errors = new List<GSheetValidationError>();

        for (var i = 0; i < rows.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(rows[i].Name))
                errors.Add(new GSheetValidationError(i, nameof(BonusRow.Name), "Name is required"));
            if (rows[i].Amount <= 0)
                errors.Add(new GSheetValidationError(i, nameof(BonusRow.Amount), "Amount must be positive"));
        }

        return errors.Count == 0 ? GSheetValidationResult.Ok() : GSheetValidationResult.Fail(errors);
    }

    public IEnumerable<BonusConfig> MapToDomain(
        IReadOnlyList<BonusRow> rows,
        GSheetImportContext context)
    {
        return rows.Select(r => new BonusConfig(r.Name, r.Amount, r.Currency, r.MinLevel, r.IsActive));
    }

    public Task<bool> ImportAsync(IEnumerable<BonusConfig> domains, GSheetImportContext context)
    {
        // In a real application you would persist to your store here.
        // The framework handles domain caching internally.
        return Task.FromResult(true);
    }
}

public sealed class BonusRowMap : ClassMap<BonusRow>
{
    public BonusRowMap()
    {
        Map(m => m.Name).Name("Name");
        Map(m => m.Amount).Name("Amount");
        Map(m => m.Currency).Name("Currency");
        Map(m => m.MinLevel).Name("MinLevel");
        Map(m => m.IsActive).Name("IsActive");
    }
}
