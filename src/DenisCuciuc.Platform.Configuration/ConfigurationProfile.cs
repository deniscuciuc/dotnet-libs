namespace DenisCuciuc.Platform.Configuration;

public enum ConfigurationProfile
{
    /// <summary>
    /// Loads all layers: base, environment-specific, local override, and environment variables.
    /// Individual <see cref="PlatformConfigurationOptions"/> flags control the exact behavior.
    /// </summary>
    Default,

    /// <summary>
    /// Loads only the base file and environment variables. No environment-specific or local layers.
    /// Useful for lightweight or embedded scenarios.
    /// </summary>
    Minimal,

    /// <summary>
    /// Loads the base and environment-specific files. Skips the local override file.
    /// Environment variables are always enabled. Suited for production / cloud deployments.
    /// </summary>
    Cloud,

    /// <summary>
    /// Loads all layers including the local override file.
    /// Intended for local development; equivalent to Default with <c>IncludeLocal = true</c>.
    /// </summary>
    LocalDev
}
