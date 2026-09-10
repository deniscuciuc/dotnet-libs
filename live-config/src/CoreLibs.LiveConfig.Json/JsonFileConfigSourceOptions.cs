namespace CoreLibs.LiveConfig.Json;

/// <summary>
/// Options for <see cref="JsonFileConfigSource"/>.
/// </summary>
public sealed class JsonFileConfigSourceOptions
{
    public const string SectionPath = "LiveConfig:Json";

    /// <summary>
    /// Root directory containing JSON config files.
    /// Files are expected as <c>{configType}.json</c>.
    /// </summary>
    public string Directory { get; set; } = "configs";

    /// <summary>
    /// Optional environment name for layered config.
    /// When set, <c>{configType}.{Environment}.json</c> overrides base file.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// File search pattern for config discovery.
    /// </summary>
    public string SearchPattern { get; set; } = "*.json";
}
