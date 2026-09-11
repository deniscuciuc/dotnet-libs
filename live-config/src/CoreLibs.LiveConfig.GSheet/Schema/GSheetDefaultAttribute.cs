namespace CoreLibs.LiveConfig.GSheet.Schema;

/// <summary>
/// Provides a default value for a column when the cell is empty or whitespace.
/// The value is converted to the property type by CsvHelper.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class GSheetDefaultAttribute(object? value) : Attribute
{
    public object? Value { get; } = value;
}
