namespace CoreLibs.LiveConfig.GSheet.Schema;

/// <summary>
/// Excludes a property from GSheet column auto-mapping.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class GSheetIgnoreAttribute : Attribute;
