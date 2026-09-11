namespace CoreLibs.LiveConfig.GSheet.Entity;

/// <summary>
/// Base class for entity configuration where the domain model IS the row type (Tier 1/2).
/// Override <see cref="Configure"/> to add validation rules, ordering, or relationships.
/// </summary>
/// <typeparam name="TDomain">The domain model type (also used as the row type).</typeparam>
public abstract class GSheetEntityConfig<TDomain>
{
    /// <summary>
    /// Configures validation, ordering, and relationships for the entity.
    /// </summary>
    public abstract void Configure(GSheetEntityBuilder<TDomain> builder);
}

/// <summary>
/// Base class for entity configuration with a separate row and domain type (Tier 3).
/// Use when the row structure differs from the domain model (e.g., aggregation, HasMany).
/// </summary>
/// <typeparam name="TRow">The GSheet row model type.</typeparam>
/// <typeparam name="TDomain">The domain model type.</typeparam>
public abstract class GSheetEntityConfig<TRow, TDomain>
{
    /// <summary>
    /// Configures sheet binding, validation, mapping, and relationships for the entity.
    /// </summary>
    public abstract void Configure(GSheetEntityBuilder<TRow, TDomain> builder);
}
