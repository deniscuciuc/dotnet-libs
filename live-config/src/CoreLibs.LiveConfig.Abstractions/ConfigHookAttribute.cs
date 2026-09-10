namespace CoreLibs.LiveConfig;

/// <summary>
/// Marks a class as an auto-discoverable <see cref="IConfigHook"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConfigHookAttribute : Attribute;
