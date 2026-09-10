namespace CoreLibs.LiveConfig;

/// <summary>
/// Marks a class as an auto-discoverable <see cref="IConfigApplier"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConfigApplierAttribute(string configType) : Attribute
{
    public string ConfigType { get; } = configType;
    public int Priority { get; init; }
}
