using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreLibs.LiveConfig;

/// <summary>
/// Shared JSON serialization options for config data.
/// </summary>
public static class ConfigJsonOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
