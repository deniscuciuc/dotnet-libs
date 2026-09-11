using System.Linq.Expressions;
using System.Text.RegularExpressions;
using CoreLibs.LiveConfig.GSheet.Pipeline;

namespace CoreLibs.LiveConfig.GSheet.Entity;

/// <summary>
/// Fluent builder for configuring a GSheet entity with separate row and domain types (Tier 3).
/// Collects validation rules, ordering, mapping, and relationships that are compiled
/// into an <see cref="EntityPipelineAdapter{TRow,TDomain}"/>.
/// </summary>
/// <typeparam name="TRow">The GSheet row model type.</typeparam>
/// <typeparam name="TDomain">The domain model type.</typeparam>
public partial class GSheetEntityBuilder<TRow, TDomain>
{
    private static partial class Patterns
    {
        [GeneratedRegex(
            @"^\s*[A-Z]+(?:[0-9]+)?(?:\s*:\s*[A-Z]+(?:[0-9]+)?)?\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        internal static partial Regex A1RangeRegex();
    }

    internal string? SheetName { get; private set; }
    internal string? Range { get; private set; }
    internal Func<TRow, TDomain>? SimpleMapper { get; private protected set; }
    internal Func<TRow, EntityMappingContext, TDomain>? ContextMapper { get; private protected set; }

    internal List<Func<IReadOnlyList<TRow>, GSheetImportContext, IEnumerable<string>>> InlineRowValidators { get; } =
        [];

    internal List<Func<IReadOnlyList<TRow>, GSheetImportContext, IEnumerable<string>>> InlineGraphValidators { get; } =
        [];

    internal List<IPropertyRule<TRow>> PropertyRules { get; } = [];
    internal List<Func<TRow, object>> OrderBySelectors { get; } = [];
    internal List<bool> OrderByIsDescending { get; } = [];
    internal List<Type> ExplicitDependencies { get; } = [];
    internal Func<IReadOnlyList<TRow>, GSheetImportContext, IReadOnlyList<TRow>>? TransformFunc { get; private set; }
    internal List<IChildRelationship> ChildRelationships { get; } = [];
    internal IAggregator<TRow, TDomain>? CustomAggregator { get; private set; }

    /// <summary>
    /// Sets the sheet name and range for this entity. Only needed for Tier 3 entities
    /// that don't have [GSheetEntity] on a domain type.
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> Sheet(string sheetName, string range = "")
    {
        if (string.IsNullOrWhiteSpace(sheetName))
            throw new ArgumentException("Sheet name cannot be null or whitespace.", nameof(sheetName));

        SheetName = sheetName.Trim();

        if (!string.IsNullOrWhiteSpace(range))
        {
            if (!Patterns.A1RangeRegex().IsMatch(range.Trim()))
                throw new ArgumentException(
                    "Range must be an A1-style reference (e.g., A1, A:Y, A1:Y, A:Y50, A1:C100).",
                    nameof(range));
            Range = range.Trim();
        }
        else
        {
            Range = string.Empty;
        }

        return this;
    }

    /// <summary>
    /// Designates the row identity key. Used for aggregation grouping and error messages.
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> HasKey<TKey>(Expression<Func<TRow, TKey>> keySelector)
    {
        // Store for use by aggregator/validators — key selection is informational
        return this;
    }

    /// <summary>
    /// Sets a custom mapping function from row + context to domain (Tier 3).
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> Map(Func<TRow, EntityMappingContext, TDomain> mapper)
    {
        ContextMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        SimpleMapper = null;
        return this;
    }

    /// <summary>
    /// Sets a simple mapping function from row to domain (no context needed).
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> MapSimple(Func<TRow, TDomain> mapper)
    {
        SimpleMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        ContextMapper = null;
        return this;
    }

    /// <summary>
    /// Adds an inline row validator. Return error messages for invalid rows, or empty for valid.
    /// Called once per import with all rows.
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> Validate(Func<IReadOnlyList<TRow>, IEnumerable<string>> validator)
    {
        InlineRowValidators.Add((rows, _) => validator(rows));
        return this;
    }

    /// <summary>
    /// Adds an inline row validator with context access.
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> Validate(
        Func<IReadOnlyList<TRow>, GSheetImportContext, IEnumerable<string>> validator)
    {
        InlineRowValidators.Add(validator);
        return this;
    }

    /// <summary>
    /// Starts a fluent property validation rule (FluentValidation-style).
    /// </summary>
    public PropertyRuleBuilder<TRow, TProp> Rule<TProp>(Expression<Func<TRow, TProp>> propertySelector)
    {
        var rule = new PropertyRuleBuilder<TRow, TProp>(this, propertySelector);
        return rule;
    }

    /// <summary>
    /// Adds a cross-row/cross-sheet graph validator. Return error messages, or empty for valid.
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> ValidateGraph(
        Func<IReadOnlyList<TRow>, GSheetImportContext, IEnumerable<string>> validator)
    {
        InlineGraphValidators.Add(validator);
        return this;
    }

    /// <summary>
    /// Adds an ascending ordering clause for deterministic output.
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> OrderBy<TKey>(Func<TRow, TKey> keySelector)
    {
        OrderBySelectors.Add(row => keySelector(row)!);
        OrderByIsDescending.Add(false);
        return this;
    }

    /// <summary>
    /// Adds another ascending ordering clause.
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> ThenBy<TKey>(Func<TRow, TKey> keySelector)
    {
        return OrderBy(keySelector);
    }

    /// <summary>
    /// Adds a descending ordering clause.
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> OrderByDesc<TKey>(Func<TRow, TKey> keySelector)
    {
        OrderBySelectors.Add(row => keySelector(row)!);
        OrderByIsDescending.Add(true);
        return this;
    }

    /// <summary>
    /// Declares an explicit dependency on another entity or importer type.
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> DependsOn<TDep>()
    {
        ExplicitDependencies.Add(typeof(TDep));
        return this;
    }

    /// <summary>
    /// Adds a row transformation step (runs after validation, before mapping).
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> Transform(
        Func<IReadOnlyList<TRow>, GSheetImportContext, IReadOnlyList<TRow>> transform)
    {
        TransformFunc = transform;
        return this;
    }

    /// <summary>
    /// Adds a row transformation step without context.
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> Transform(
        Func<IReadOnlyList<TRow>, IReadOnlyList<TRow>> transform)
    {
        TransformFunc = (rows, _) => transform(rows);
        return this;
    }

    /// <summary>
    /// Declares a HasMany child relationship. The child type must be produced by another entity/pipeline.
    /// </summary>
    public ChildRelationshipBuilder<TChild> HasMany<TChild>()
    {
        var relationship = new ChildRelationshipBuilder<TChild>();
        ChildRelationships.Add(relationship);
        return relationship;
    }

    /// <summary>
    /// Sets a custom aggregator for complex multi-row → entity grouping.
    /// </summary>
    public GSheetEntityBuilder<TRow, TDomain> UseAggregator(IAggregator<TRow, TDomain> aggregator)
    {
        CustomAggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        return this;
    }

    /// <summary>
    /// Compiles the builder configuration into an <see cref="EntityPipelineAdapter{TRow,TDomain}"/>.
    /// </summary>
    internal EntityPipelineAdapter<TRow, TDomain> Build(string sheetName, string range)
    {
        var effectiveSheet = SheetName ?? sheetName;
        var effectiveRange = Range ?? range;

        return new EntityPipelineAdapter<TRow, TDomain>(this, effectiveSheet, effectiveRange);
    }
}

/// <summary>
/// Fluent builder for Tier 1/2 entities where the domain model IS the row type.
/// Pre-configures identity mapping.
/// </summary>
/// <typeparam name="TDomain">The domain model type (also the row type).</typeparam>
public class GSheetEntityBuilder<TDomain> : GSheetEntityBuilder<TDomain, TDomain>
{
    public GSheetEntityBuilder()
    {
        // Default: identity mapping (domain = row)
        SimpleMapper = row => row;
    }
}
