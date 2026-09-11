namespace CoreLibs.LiveConfig;

/// <summary>
/// Marks a class as an auto-discoverable <see cref="IConfigSource"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConfigSourceAttribute(string sourceName) : Attribute
{
    public string SourceName { get; } = sourceName;
}
