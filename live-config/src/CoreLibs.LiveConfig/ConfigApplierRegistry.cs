using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig;

/// <summary>
/// Registry that maps config type strings to ordered lists of <see cref="IConfigApplier"/> instances.
/// Supports multiple appliers per config type, sorted by priority.
/// Thread-safe for both registration and resolution.
/// </summary>
public sealed class ConfigApplierRegistry
{
    private readonly ConcurrentDictionary<string, ImmutableList<IConfigApplier>> _appliers =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(IConfigApplier applier)
    {
        _appliers.AddOrUpdate(
            applier.ConfigType,
            _ => [applier],
            (_, list) => list.Add(applier).Sort((a, b) => a.Priority.CompareTo(b.Priority)));
    }

    /// <summary>
    /// Resolves all appliers for a given config type, ordered by priority.
    /// </summary>
    public IReadOnlyList<IConfigApplier> Resolve(string configType)
    {
        return _appliers.TryGetValue(configType, out var list) ? list : [];
    }

    /// <summary>
    /// All registered config types.
    /// </summary>
    public IReadOnlyCollection<string> ConfigTypes => _appliers.Keys.ToList();

    /// <summary>
    /// All registered appliers across all config types.
    /// </summary>
    public IEnumerable<IConfigApplier> All => _appliers.Values.SelectMany(x => x);

    /// <summary>
    /// Applies config data to all registered appliers for a config type.
    /// </summary>
    public async Task ApplyAsync(string configType, string json, int version, ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var appliers = Resolve(configType);
        if (appliers.Count == 0)
        {
            logger.LogDebug("No appliers registered for config type {ConfigType}", configType);
            return;
        }

        foreach (var applier in appliers)
            try
            {
                await applier.ApplyAsync(json, version, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Applier {Applier} failed for config type {ConfigType} v{Version}",
                    applier.GetType().Name, configType, version);
            }
    }
}
