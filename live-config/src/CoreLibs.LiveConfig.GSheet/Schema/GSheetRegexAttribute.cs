namespace CoreLibs.LiveConfig.GSheet.Schema;

/// <summary>
/// Validates that a string property value matches the specified regex pattern.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class GSheetRegexAttribute(string pattern) : Attribute
{
    public string Pattern { get; } = pattern ?? throw new ArgumentNullException(nameof(pattern));
}
