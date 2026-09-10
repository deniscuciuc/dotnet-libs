namespace CoreLibs.LiveConfig.GSheet.Entity;

/// <summary>
/// Marker interface for type-erased child relationships.
/// </summary>
public interface IChildRelationship
{
    /// <summary>
    /// The child domain type produced by another entity/pipeline.
    /// </summary>
    Type ChildType { get; }

    /// <summary>
    /// Groups child data from the import context by FK value.
    /// Returns a Dictionary{Type, object} entry where object is ILookup{object, TChild}.
    /// </summary>
    object GroupChildren(GSheetImportContext context);
}

/// <summary>
/// Configures a HasMany child relationship. The child type must be produced by another entity/pipeline.
/// </summary>
public sealed class ChildRelationshipBuilder<TChild> : IChildRelationship
{
    private Func<TChild, object>? _foreignKeySelector;
    private Func<TChild, object>? _orderBySelector;

    public Type ChildType => typeof(TChild);

    /// <summary>
    /// Sets the foreign key selector on the child entity.
    /// </summary>
    public ChildRelationshipBuilder<TChild> WithForeignKey<TKey>(Func<TChild, TKey> keySelector) where TKey : notnull
    {
        _foreignKeySelector = child => keySelector(child);
        return this;
    }

    /// <summary>
    /// Sets ordering for child entities within each group.
    /// </summary>
    public ChildRelationshipBuilder<TChild> Ordered<TSortKey>(Func<TChild, TSortKey> sortSelector)
    {
        _orderBySelector = child => sortSelector(child)!;
        return this;
    }

    object IChildRelationship.GroupChildren(GSheetImportContext context)
    {
        var children = context.GetOptionalDomain<TChild>();

        if (_foreignKeySelector is null)
            return Enumerable.Empty<TChild>().ToLookup(object (_) => "__all__");

        IEnumerable<TChild> ordered = _orderBySelector is not null
            ? children.OrderBy(c => _orderBySelector(c))
            : children;

        return ordered.ToLookup(c => _foreignKeySelector(c));
    }
}
