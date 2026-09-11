using CoreLibs.LiveConfig.Examples.Worker.Domain;
using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.Examples.Worker.Sources.GSheet.Pipeline;

// ── Row DTO (auto-mapped via attributes) ──────────────────────
// [GSheetColumn] without a name uses the property name as the column header.

public record CurrencyRow(
    [property: GSheetColumn]
    [property: GSheetRequired]
    string Code,
    [property: GSheetColumn]
    [property: GSheetRequired]
    string Name,
    [property: GSheetColumn]
    [property: GSheetRange(0, 8)]
    int DecimalPlaces);

// ── Pipeline ──────────────────────────────────────────────────

[GSheetImporter("Currencies", "A1:C")]
public sealed class CurrencyImporter : IGSheetPipeline<CurrencyRow, CurrencyConfig>
{
    public IEnumerable<CurrencyConfig> MapToDomain(
        IReadOnlyList<CurrencyRow> rows,
        GSheetImportContext context)
    {
        return rows
            .OrderBy(r => r.Code, StringComparer.Ordinal)
            .Select(r => new CurrencyConfig(r.Code, r.Name, r.DecimalPlaces));
    }
}
