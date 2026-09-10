using CsvHelper.Configuration;

namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Defines a declarative, staged GSheet import pipeline.
/// All stages are optional via default interface members — only implement what you need.
/// <para>
/// Pipeline execution order:
/// <list type="number">
///   <item>Structural validation (automatic from attributes)</item>
///   <item><see cref="CreateMapper"/> or auto-map from attributes</item>
///   <item>Attribute-based row validation (automatic)</item>
///   <item><see cref="RowValidator"/> (custom row rules)</item>
///   <item><see cref="Transformer"/> (enrich/normalize rows)</item>
///   <item><see cref="GraphValidator"/> (cross-row/cross-sheet rules)</item>
///   <item>Reference resolution (automatic from <c>[GSheetRef]</c> attributes)</item>
///   <item><see cref="Aggregator"/> or <see cref="MapToDomain"/></item>
///   <item><see cref="ProcessAsync"/></item>
/// </list>
/// </para>
/// </summary>
/// <typeparam name="TRow">The GSheet row model type used by CsvHelper.</typeparam>
/// <typeparam name="TDomain">The domain model type.</typeparam>
public interface IGSheetPipeline<TRow, TDomain>
{
    /// <summary>
    /// Creates the CsvHelper ClassMap for parsing. Return <c>null</c> to use
    /// attribute-based auto-mapping (recommended).
    /// </summary>
    ClassMap<TRow>? CreateMapper()
    {
        return null;
    }

    /// <summary>
    /// Optional custom row-level validator.
    /// </summary>
    IRowValidator<TRow>? RowValidator => null;

    /// <summary>
    /// Optional cross-row/cross-sheet graph validator.
    /// </summary>
    IGraphValidator<TRow>? GraphValidator => null;

    /// <summary>
    /// Optional row transformer — enrich, normalize, or filter rows before domain mapping.
    /// </summary>
    IRowTransformer<TRow>? Transformer => null;

    /// <summary>
    /// Optional aggregator for multi-row → entity grouping.
    /// When set, this is used instead of <see cref="MapToDomain"/>.
    /// </summary>
    IAggregator<TRow, TDomain>? Aggregator => null;

    /// <summary>
    /// Maps parsed rows to domain models. Used when <see cref="Aggregator"/> is <c>null</c>.
    /// Default implementation maps rows 1:1 — override for custom logic.
    /// </summary>
    IEnumerable<TDomain> MapToDomain(IReadOnlyList<TRow> rows, GSheetImportContext context)
    {
        return Aggregator?.Aggregate(rows, context) ?? throw new InvalidOperationException(
            $"Pipeline {GetType().Name} must either set an Aggregator or override MapToDomain.");
    }

    /// <summary>
    /// Processes the mapped domain models. Override to persist, publish, or perform side effects.
    /// Returns a pipeline result with data, errors, and warnings.
    /// </summary>
    Task<GSheetPipelineResult<TDomain>> ProcessAsync(
        IReadOnlyList<TDomain> domains,
        GSheetImportContext context)
    {
        return Task.FromResult(
            GSheetPipelineResult<TDomain>.Success(domains, domains.Count, domains.Count));
    }
}
