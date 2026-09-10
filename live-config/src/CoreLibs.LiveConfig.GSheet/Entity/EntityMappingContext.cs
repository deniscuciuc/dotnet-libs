namespace CoreLibs.LiveConfig.GSheet.Entity;

/// <summary>
/// Provides access to child relationship data and cross-entity resolution during mapping.
/// Wraps <see cref="GSheetImportContext"/> with pre-grouped child collections.
/// </summary>
public sealed class EntityMappingContext
{
    private readonly GSheetImportContext _importContext;
    private readonly Dictionary<Type, object> _childGroups;

    internal EntityMappingContext(GSheetImportContext importContext, Dictionary<Type, object> childGroups)
    {
        _importContext = importContext ?? throw new ArgumentNullException(nameof(importContext));
        _childGroups = childGroups ?? throw new ArgumentNullException(nameof(childGroups));
    }

    /// <summary>
    /// Gets children of type <typeparamref name="TChild"/> that match the given foreign key value.
    /// Returns an empty list if no children match.
    /// </summary>
    public IReadOnlyList<TChild> Children<TChild>(object key)
    {
        if (!_childGroups.TryGetValue(typeof(TChild), out var groupObj))
            return [];

        var lookup = (ILookup<object, TChild>)groupObj;
        return lookup[key].ToList();
    }

    /// <summary>
    /// Resolves a domain entity from the import context by key.
    /// </summary>
    public TDomain? Resolve<TDomain>(string key)
    {
        var domains = _importContext.GetDomain<TDomain>();
        return domains is null ? default : domains.FirstOrDefault();
    }

    /// <summary>
    /// Resolves a required domain entity from the import context.
    /// Throws if the upstream data is not available.
    /// </summary>
    public IReadOnlyList<TDomain> ResolveRequired<TDomain>()
    {
        return _importContext.GetRequiredDomain<TDomain>();
    }

    /// <summary>
    /// Gets all domain data of the specified type from the import context.
    /// </summary>
    public IReadOnlyList<TDomain> GetDomain<TDomain>()
    {
        return _importContext.GetOptionalDomain<TDomain>();
    }
}
