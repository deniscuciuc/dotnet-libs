namespace DenisCuciuc.Platform.Configuration;

public sealed class PlatformConfigurationOptions
{
    /// <summary>Base file name without extension. Defaults to <c>appsettings</c>.</summary>
    public string BaseFileName { get; set; } = "appsettings";

    /// <summary>File extension without the leading dot. Defaults to <c>yml</c>.</summary>
    public string Extension { get; set; } = "yml";

    /// <summary>
    /// Preset that drives the file-inclusion strategy.
    /// When set to anything other than <see cref="ConfigurationProfile.Default"/> the profile
    /// determines which layers are loaded, overriding the individual <c>Include*</c> flags.
    /// </summary>
    public ConfigurationProfile Profile { get; set; } = ConfigurationProfile.Default;

    /// <summary>Whether to add an environment-specific file, e.g. <c>appsettings.Production.yml</c>.</summary>
    public bool IncludeEnvironment { get; set; } = true;

    /// <summary>Whether to add the local override file, e.g. <c>appsettings.Local.yml</c>.</summary>
    public bool IncludeLocal { get; set; } = false;

    /// <summary>Whether to add environment variables as a configuration source.</summary>
    public bool IncludeEnvironmentVariables { get; set; } = true;

    /// <summary>
    /// Prefix filter applied when loading environment variables.
    /// Only variables whose names start with this prefix are loaded, and the prefix is stripped.
    /// Set to <c>null</c> or empty to load all environment variables.
    /// Defaults to no prefix.
    /// </summary>
    public string? EnvironmentVariablesPrefix { get; set; }

    /// <summary>Whether the base configuration file is optional. Defaults to <c>true</c>.</summary>
    public bool Optional { get; set; } = true;

    /// <summary>Whether to reload configuration when the source file changes. Defaults to <c>true</c>.</summary>
    public bool ReloadOnChange { get; set; } = true;
}
