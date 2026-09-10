namespace CoreLibs.LiveConfig.GSheet.Schema;

/// <summary>
/// Validates that a numeric property value falls within the specified range (inclusive).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class GSheetRangeAttribute(double minimum, double maximum) : Attribute
{
    public double Minimum { get; } = minimum;
    public double Maximum { get; } = maximum;
}
