namespace CoreLibs.LiveConfig;

/// <summary>
/// Applies configuration data to an in-memory representation or downstream system.
/// Multiple appliers can be registered for the same config type.
/// </summary>
public interface IConfigApplier
{
    /// <summary>
    /// The configuration type this applier handles.
    /// </summary>
    string ConfigType { get; }

    /// <summary>
    /// Priority for ordering when multiple appliers handle the same config type.
    /// Lower values run first. Default is 0.
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// Applies the config data.
    /// </summary>
    Task ApplyAsync(string json, int version, CancellationToken cancellationToken = default);
}
