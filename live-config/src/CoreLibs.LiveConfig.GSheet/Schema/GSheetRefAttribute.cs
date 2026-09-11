namespace CoreLibs.LiveConfig.GSheet.Schema;

/// <summary>
/// Declares a cross-sheet reference on a row property. The pipeline will auto-validate
/// that the referenced domain contains an entity matching this property's value.
/// Also automatically infers dependency ordering (Needs) in the import graph.
/// </summary>
/// <param name="referencedDomainType">The domain type to look up in the import context.</param>
/// <param name="keyProperty">
/// The property name on the referenced domain type to match against this property's value.
/// </param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class GSheetRefAttribute(Type referencedDomainType, string keyProperty) : Attribute
{
    /// <summary>
    /// The domain type produced by an upstream importer (e.g., <c>typeof(CurrencyConfig)</c>).
    /// </summary>
    public Type ReferencedDomainType { get; } =
        referencedDomainType ?? throw new ArgumentNullException(nameof(referencedDomainType));

    /// <summary>
    /// The property name on the referenced domain used as the lookup key
    /// (e.g., <c>nameof(CurrencyConfig.Code)</c>).
    /// </summary>
    public string KeyProperty { get; } = keyProperty ?? throw new ArgumentNullException(nameof(keyProperty));
}
