namespace CoreLibs.LiveConfig.GSheet.Schema;

/// <summary>
/// Marks a property as the row identity key.
/// Used by aggregators for grouping, by validators for duplicate detection,
/// and in error messages for row identification.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class GSheetRowKeyAttribute : Attribute;
