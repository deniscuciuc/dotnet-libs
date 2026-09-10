namespace CoreLibs.LiveConfig.GSheet.Schema;

/// <summary>
/// Marks a property as required. The attribute-based row validator will produce
/// an error if the value is null, empty, or whitespace (for strings) or default (for value types).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class GSheetRequiredAttribute : Attribute;
