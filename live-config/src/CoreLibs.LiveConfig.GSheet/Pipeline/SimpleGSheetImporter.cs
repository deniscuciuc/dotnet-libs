using CsvHelper.Configuration;

namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Simplified base class for the common case where each row maps 1:1 to a domain entity.
/// Just implement <see cref="Map"/> — everything else (ClassMap, validation) is auto-wired
/// from attributes on <typeparamref name="TRow"/>.
/// <para>
/// Example:
/// <code>
/// [GSheetImporter("Bonuses", "A1:D")]
/// public class BonusesImporter : SimpleGSheetImporter&lt;BonusRow, BonusConfig&gt;
/// {
///     protected override BonusConfig Map(BonusRow row)
///         =&gt; new(row.Name, row.Amount, row.Currency, row.MinLevel);
/// }
/// </code>
/// </para>
/// </summary>
public abstract class SimpleGSheetImporter<TRow, TDomain> : IGSheetPipeline<TRow, TDomain>
{
    /// <summary>
    /// Maps a single row to a domain entity.
    /// </summary>
    protected abstract TDomain Map(TRow row);

    public ClassMap<TRow>? CreateMapper()
    {
        // auto-map from attributes
        return null;
    }

    public IRowValidator<TRow>? RowValidator => null;
    public IGraphValidator<TRow>? GraphValidator => null;
    public IRowTransformer<TRow>? Transformer => null;
    public IAggregator<TRow, TDomain>? Aggregator => null;

    public IEnumerable<TDomain> MapToDomain(IReadOnlyList<TRow> rows, GSheetImportContext context)
    {
        return rows.Select(Map);
    }

    public Task<GSheetPipelineResult<TDomain>> ProcessAsync(
        IReadOnlyList<TDomain> domains,
        GSheetImportContext context)
    {
        return Task.FromResult(
            GSheetPipelineResult<TDomain>.Success(domains, domains.Count, domains.Count));
    }
}
