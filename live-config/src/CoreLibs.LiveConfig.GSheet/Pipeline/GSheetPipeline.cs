using CsvHelper.Configuration;

namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Abstract base class for GSheet pipelines with a builder-style <see cref="Configure"/> method.
/// Provides <c>UseValidator</c>, <c>UseGraphValidator</c>, <c>UseTransformer</c>, and
/// <c>UseAggregator</c> for composing pipeline stages.
/// <para>
/// Example:
/// <code>
/// [GSheetImporter("Quests", "A1:E")]
/// public class QuestPipeline : GSheetPipeline&lt;QuestRow, QuestConfig&gt;
/// {
///     protected override void Configure()
///     {
///         UseValidator&lt;QuestRowValidator&gt;();
///         UseAggregator&lt;QuestAggregator&gt;();
///     }
/// }
/// </code>
/// </para>
/// </summary>
public abstract class GSheetPipeline<TRow, TDomain> : IGSheetPipeline<TRow, TDomain>
{
    private IRowValidator<TRow>? _rowValidator;
    private IGraphValidator<TRow>? _graphValidator;
    private IRowTransformer<TRow>? _transformer;
    private IAggregator<TRow, TDomain>? _aggregator;
    private bool _configured;

    private void EnsureConfigured()
    {
        if (_configured) return;
        Configure();
        _configured = true;
    }

    /// <summary>
    /// Override to compose pipeline stages using the <c>Use*</c> methods.
    /// Called once before the first pipeline execution.
    /// </summary>
    protected virtual void Configure()
    {
    }

    /// <summary>
    /// Registers a custom row validator. Multiple calls chain into a composite.
    /// </summary>
    protected void UseValidator<T>() where T : IRowValidator<TRow>, new()
    {
        UseValidator(new T());
    }

    /// <summary>
    /// Registers a custom row validator instance. Multiple calls chain into a composite.
    /// </summary>
    protected void UseValidator(IRowValidator<TRow> validator)
    {
        if (_rowValidator is null)
        {
            _rowValidator = validator;
        }
        else
        {
            if (_rowValidator is not CompositeRowValidator<TRow> composite)
            {
                composite = new CompositeRowValidator<TRow>();
                composite.Add(_rowValidator);
            }

            composite.Add(validator);
            _rowValidator = composite;
        }
    }

    /// <summary>
    /// Registers a custom graph validator. Multiple calls chain into a composite.
    /// </summary>
    protected void UseGraphValidator<T>() where T : IGraphValidator<TRow>, new()
    {
        UseGraphValidator(new T());
    }

    /// <summary>
    /// Registers a custom graph validator instance. Multiple calls chain into a composite.
    /// </summary>
    protected void UseGraphValidator(IGraphValidator<TRow> validator)
    {
        if (_graphValidator is null)
        {
            _graphValidator = validator;
        }
        else
        {
            if (_graphValidator is not CompositeGraphValidator<TRow> composite)
            {
                composite = new CompositeGraphValidator<TRow>();
                composite.Add(_graphValidator);
            }

            composite.Add(validator);
            _graphValidator = composite;
        }
    }

    /// <summary>
    /// Registers a row transformer.
    /// </summary>
    protected void UseTransformer<T>() where T : IRowTransformer<TRow>, new()
    {
        _transformer = new T();
    }

    /// <summary>
    /// Registers a row transformer instance.
    /// </summary>
    protected void UseTransformer(IRowTransformer<TRow> transformer)
    {
        _transformer = transformer;
    }

    /// <summary>
    /// Registers an aggregator for multi-row → entity grouping.
    /// When set, <see cref="MapToDomain"/> delegates to the aggregator automatically.
    /// </summary>
    protected void UseAggregator<T>() where T : IAggregator<TRow, TDomain>, new()
    {
        _aggregator = new T();
    }

    /// <summary>
    /// Registers an aggregator instance.
    /// </summary>
    protected void UseAggregator(IAggregator<TRow, TDomain> aggregator)
    {
        _aggregator = aggregator;
    }

    // ── IGSheetPipeline<TRow, TDomain> implementation ──

    public virtual ClassMap<TRow>? CreateMapper()
    {
        return null;
    }

    public IRowValidator<TRow>? RowValidator
    {
        get
        {
            EnsureConfigured();
            return _rowValidator;
        }
    }

    public IGraphValidator<TRow>? GraphValidator
    {
        get
        {
            EnsureConfigured();
            return _graphValidator;
        }
    }

    public IRowTransformer<TRow>? Transformer
    {
        get
        {
            EnsureConfigured();
            return _transformer;
        }
    }

    public IAggregator<TRow, TDomain>? Aggregator
    {
        get
        {
            EnsureConfigured();
            return _aggregator;
        }
    }

    /// <summary>
    /// Maps rows to domain models. When an aggregator is configured, delegates to it.
    /// Override for custom 1:1 or custom mapping logic without an aggregator.
    /// </summary>
    public virtual IEnumerable<TDomain> MapToDomain(IReadOnlyList<TRow> rows, GSheetImportContext context)
    {
        EnsureConfigured();
        if (_aggregator is not null)
            return _aggregator.Aggregate(rows, context);

        throw new InvalidOperationException(
            $"Pipeline {GetType().Name} must either set an Aggregator via UseAggregator<T>() or override MapToDomain.");
    }

    /// <summary>
    /// Processes domain models. Default returns success with all data.
    /// Override to persist, publish, or perform side effects.
    /// </summary>
    public virtual Task<GSheetPipelineResult<TDomain>> ProcessAsync(
        IReadOnlyList<TDomain> domains,
        GSheetImportContext context)
    {
        return Task.FromResult(
            GSheetPipelineResult<TDomain>.Success(domains, domains.Count, domains.Count));
    }
}
