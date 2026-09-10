using System.Collections.Concurrent;

namespace CoreLibs.LiveConfig.GSheet;

public sealed class GSheetImportContext
{
    private readonly ConcurrentDictionary<Type, object> _domainByType = new();
    private readonly ConcurrentDictionary<Type, object> _rowsByType = new();

    /// <summary>
    /// The import report for the current run. Populated by the orchestrator.
    /// </summary>
    public GSheetImportReport Report { get; } = new();

    /// <summary>
    /// The sheet name currently being processed. Set by the orchestrator before each pipeline.
    /// </summary>
    public string? SheetName { get; internal set; }

    internal void Clear()
    {
        _domainByType.Clear();
        _rowsByType.Clear();
        Report.Clear();
        SheetName = null;
    }

    internal void SetRows<TRow>(IReadOnlyList<TRow> rows)
    {
        _rowsByType[typeof(TRow)] = rows;
    }

    internal void SetDomain<TDomain>(IEnumerable<TDomain> domain)
    {
        _domainByType[typeof(TDomain)] = domain;
    }

    public IReadOnlyList<TRow>? GetRows<TRow>()
    {
        return _rowsByType.TryGetValue(typeof(TRow), out var o) ? (IReadOnlyList<TRow>)o : null;
    }

    public IEnumerable<TDomain>? GetDomain<TDomain>()
    {
        return _domainByType.TryGetValue(typeof(TDomain), out var o) ? (IEnumerable<TDomain>)o : null;
    }

    public object? GetDomain(Type domainType)
    {
        return _domainByType.GetValueOrDefault(domainType);
    }

    public object? GetRows(Type rowType)
    {
        return _rowsByType.GetValueOrDefault(rowType);
    }

    /// <summary>
    /// Gets domain data for the specified type. Throws if the upstream importer
    /// has not run or produced no data — use when the dependency is mandatory.
    /// </summary>
    public IReadOnlyList<TDomain> GetRequiredDomain<TDomain>()
    {
        var data = GetDomain<TDomain>();
        if (data is null)
            throw new InvalidOperationException(
                $"Required domain data for '{typeof(TDomain).Name}' not found in import context. " +
                $"Ensure the upstream importer runs before this pipeline (check 'Needs' dependencies).");

        return data as IReadOnlyList<TDomain> ?? data.ToList();
    }

    /// <summary>
    /// Gets domain data for the specified type, returning an empty list if not available.
    /// Use when the dependency is optional.
    /// </summary>
    public IReadOnlyList<TDomain> GetOptionalDomain<TDomain>()
    {
        var data = GetDomain<TDomain>();
        if (data is null) return [];
        return data as IReadOnlyList<TDomain> ?? data.ToList();
    }

    /// <summary>
    /// Gets domain data grouped by a key selector. Returns an <see cref="ILookup{TKey, TDomain}"/>.
    /// Useful for cross-cutting aggregation (e.g., rewards grouped by tournament ID).
    /// </summary>
    public ILookup<TKey, TDomain> GetGroupedDomain<TDomain, TKey>(
        Func<TDomain, TKey> keySelector) where TKey : notnull
    {
        return GetOptionalDomain<TDomain>().ToLookup(keySelector);
    }

    /// <summary>
    /// Gets domain data indexed by a unique key. Throws on duplicate keys.
    /// Useful for lookups by ID (e.g., currencies by code).
    /// </summary>
    public IReadOnlyDictionary<TKey, TDomain> GetIndexedDomain<TDomain, TKey>(
        Func<TDomain, TKey> keySelector) where TKey : notnull
    {
        return GetOptionalDomain<TDomain>().ToDictionary(keySelector);
    }

    /// <summary>
    /// Resolves a single upstream domain entity by key.
    /// Returns <c>null</c> if the domain type or key is not found.
    /// </summary>
    public TDomain? Resolve<TDomain>(string key, Func<TDomain, string> keySelector)
        where TDomain : class
    {
        var data = GetDomain<TDomain>();
        return data?.FirstOrDefault(d => string.Equals(keySelector(d), key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Resolves a single upstream domain entity by key. Throws if not found.
    /// </summary>
    public TDomain ResolveRequired<TDomain>(string key, Func<TDomain, string> keySelector)
        where TDomain : class
    {
        return Resolve(key, keySelector)
               ?? throw new InvalidOperationException(
                   $"Cannot resolve '{typeof(TDomain).Name}' with key '{key}'. " +
                   $"Ensure the upstream importer has run and produced the expected data.");
    }
}
