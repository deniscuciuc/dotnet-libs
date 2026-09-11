using CoreLibs.LiveConfig.GSheet.Pipeline;
using CsvHelper.Configuration;

namespace CoreLibs.LiveConfig.GSheet.Entity;

/// <summary>
/// Adapts the declarative entity configuration into <see cref="IGSheetPipeline{TRow,TDomain}"/>
/// so the existing <see cref="GSheetImportOrchestrator"/> can execute entities unchanged.
/// </summary>
public sealed class EntityPipelineAdapter<TRow, TDomain> : IGSheetPipeline<TRow, TDomain>
{
    private readonly GSheetEntityBuilder<TRow, TDomain> _builder;

    /// <summary>
    /// The sheet name this entity reads from.
    /// </summary>
    public string EntitySheetName { get; }

    /// <summary>
    /// The A1-style range within the sheet.
    /// </summary>
    public string EntityRange { get; }

    internal EntityPipelineAdapter(GSheetEntityBuilder<TRow, TDomain> builder, string sheetName, string range)
    {
        _builder = builder;
        EntitySheetName = sheetName;
        EntityRange = range;

        // Build row validator from inline validators + property rules
        if (builder.InlineRowValidators.Count > 0 || builder.PropertyRules.Count > 0)
            RowValidator = new EntityRowValidator<TRow>(builder.InlineRowValidators, builder.PropertyRules);

        // Build graph validator from inline graph validators
        if (builder.InlineGraphValidators.Count > 0)
            GraphValidator = new EntityGraphValidator<TRow>(builder.InlineGraphValidators);

        // Build transformer from ordering + transform function
        if (builder.OrderBySelectors.Count > 0 || builder.TransformFunc is not null)
            Transformer = new EntityRowTransformer<TRow>(
                builder.OrderBySelectors,
                builder.OrderByIsDescending,
                builder.TransformFunc);

        // Build aggregator
        if (builder.CustomAggregator is not null)
            Aggregator = builder.CustomAggregator;
        else if (builder.ContextMapper is not null)
            Aggregator = new EntityContextAggregator<TRow, TDomain>(
                builder.ContextMapper,
                builder.ChildRelationships);
    }

    public ClassMap<TRow>? CreateMapper()
    {
        // Use attribute-based auto-mapping
        return null;
    }

    public IRowValidator<TRow>? RowValidator { get; }

    public IGraphValidator<TRow>? GraphValidator { get; }

    public IRowTransformer<TRow>? Transformer { get; }

    public IAggregator<TRow, TDomain>? Aggregator { get; }

    public IEnumerable<TDomain> MapToDomain(IReadOnlyList<TRow> rows, GSheetImportContext context)
    {
        if (Aggregator is not null)
            return Aggregator.Aggregate(rows, context);

        if (_builder.SimpleMapper is not null)
            return rows.Select(_builder.SimpleMapper);

        if (_builder.ContextMapper is not null)
        {
            var childGroups = BuildChildGroups(context);
            var mappingContext = new EntityMappingContext(context, childGroups);
            return rows.Select(row => _builder.ContextMapper(row, mappingContext));
        }

        // Fallback for Tier 1: identity mapping (TRow == TDomain)
        if (typeof(TRow) == typeof(TDomain))
            return rows.Cast<TDomain>();

        throw new InvalidOperationException(
            $"Entity adapter for {typeof(TDomain).Name} has no mapping configured. " +
            "Use Map(), MapSimple(), or ensure TRow == TDomain for Tier 1 entities.");
    }

    public Task<GSheetPipelineResult<TDomain>> ProcessAsync(
        IReadOnlyList<TDomain> domains,
        GSheetImportContext context)
    {
        return Task.FromResult(
            GSheetPipelineResult<TDomain>.Success(domains, domains.Count, domains.Count));
    }

    private Dictionary<Type, object> BuildChildGroups(GSheetImportContext context)
    {
        var groups = new Dictionary<Type, object>();
        foreach (var rel in _builder.ChildRelationships) groups[rel.ChildType] = rel.GroupChildren(context);
        return groups;
    }
}

/// <summary>
/// Applies ordering and custom transform functions as a row transformer.
/// </summary>
internal sealed class EntityRowTransformer<TRow>(
    List<Func<TRow, object>> orderBySelectors,
    List<bool> orderByIsDescending,
    Func<IReadOnlyList<TRow>, GSheetImportContext, IReadOnlyList<TRow>>? customTransform)
    : IRowTransformer<TRow>
{
    public IReadOnlyList<TRow> Transform(IReadOnlyList<TRow> rows, GSheetImportContext context)
    {
        var result = rows;

        if (customTransform is not null)
            result = customTransform(result, context);

        if (orderBySelectors.Count > 0)
            result = ApplyOrdering(result);

        return result;
    }

    private IReadOnlyList<TRow> ApplyOrdering(IReadOnlyList<TRow> rows)
    {
        IOrderedEnumerable<TRow>? ordered = null;

        for (var i = 0; i < orderBySelectors.Count; i++)
        {
            var selector = orderBySelectors[i];
            var descending = orderByIsDescending[i];

            if (i == 0)
                ordered = descending
                    ? rows.OrderByDescending(selector)
                    : rows.OrderBy(selector);
            else
                ordered = descending
                    ? ordered!.ThenByDescending(selector)
                    : ordered!.ThenBy(selector);
        }

        return ordered?.ToList() ?? rows.ToList();
    }
}

/// <summary>
/// Aggregator that uses a context-aware mapping function and pre-groups child relationships.
/// </summary>
internal sealed class EntityContextAggregator<TRow, TDomain>(
    Func<TRow, EntityMappingContext, TDomain> mapper,
    List<IChildRelationship> childRelationships)
    : IAggregator<TRow, TDomain>
{
    public IEnumerable<TDomain> Aggregate(IReadOnlyList<TRow> rows, GSheetImportContext context)
    {
        var groups = new Dictionary<Type, object>();
        foreach (var rel in childRelationships) groups[rel.ChildType] = rel.GroupChildren(context);

        var mappingContext = new EntityMappingContext(context, groups);
        return rows.Select(row => mapper(row, mappingContext));
    }
}
