using System.Collections.Concurrent;
using CoreLibs.LiveConfig;

namespace CoreLibs.Localization.Examples.Worker.Services;

/// <summary>
/// Minimal in-memory <see cref="IConfigStore"/> for the example.
/// Stores versioned config snapshots in a dictionary — no persistence across restarts.
/// </summary>
public sealed class InMemoryConfigStore : IConfigStore
{
    private readonly ConcurrentDictionary<string, List<ConfigVersion>> _versions = new();
    private readonly ConcurrentDictionary<string, int> _activeVersions = new();

    public Task<ConfigVersion?> GetActiveAsync(string configType, CancellationToken cancellationToken = default)
    {
        if (!_activeVersions.TryGetValue(configType, out var activeVersion))
            return Task.FromResult<ConfigVersion?>(null);
        return GetVersionAsync(configType, activeVersion, cancellationToken);
    }

    public Task<ConfigVersion?> GetVersionAsync(string configType, int version,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(configType, out var list))
            return Task.FromResult<ConfigVersion?>(null);
        var found = list.FirstOrDefault(v => v.Version == version);
        return Task.FromResult(found);
    }

    public Task<IReadOnlyList<ConfigVersion>> GetRecentVersionsAsync(string configType, int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(configType, out var list))
            return Task.FromResult<IReadOnlyList<ConfigVersion>>([]);
        return Task.FromResult<IReadOnlyList<ConfigVersion>>(
            list.OrderByDescending(v => v.Version).Take(count).ToList());
    }

    public Task<int> GetLatestVersionNumberAsync(string configType, CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(configType, out var list) || list.Count == 0)
            return Task.FromResult(0);
        return Task.FromResult(list.Max(v => v.Version));
    }

    public Task<string?> GetActiveHashAsync(string configType, CancellationToken cancellationToken = default)
    {
        if (!_activeVersions.TryGetValue(configType, out var activeVersion))
            return Task.FromResult<string?>(null);
        if (!_versions.TryGetValue(configType, out var list))
            return Task.FromResult<string?>(null);
        var active = list.FirstOrDefault(v => v.Version == activeVersion);
        return Task.FromResult(active?.DataHash);
    }

    public Task<ActiveVersionInfo?> GetActiveVersionInfoAsync(string configType,
        CancellationToken cancellationToken = default)
    {
        if (!_activeVersions.TryGetValue(configType, out var activeVersion))
            return Task.FromResult<ActiveVersionInfo?>(null);
        if (!_versions.TryGetValue(configType, out var list))
            return Task.FromResult<ActiveVersionInfo?>(null);
        var active = list.FirstOrDefault(v => v.Version == activeVersion);
        if (active is null) return Task.FromResult<ActiveVersionInfo?>(null);
        return Task.FromResult<ActiveVersionInfo?>(new ActiveVersionInfo(active.DataHash, active.Version));
    }

    public Task<int> CreateVersionAsync(string configType, string dataJson, string dataHash,
        string? createdBy = null, CancellationToken cancellationToken = default)
    {
        var list = _versions.GetOrAdd(configType, _ => []);
        var version = list.Count == 0 ? 1 : list.Max(v => v.Version) + 1;
        list.Add(new ConfigVersion(configType, version, dataJson, dataHash, true, DateTimeOffset.UtcNow, createdBy));
        return Task.FromResult(version);
    }

    public Task<bool> ActivateVersionAsync(string configType, int version,
        CancellationToken cancellationToken = default)
    {
        _activeVersions[configType] = version;
        return Task.FromResult(true);
    }

    public Task<IReadOnlyDictionary<string, int>> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        var manifest = _activeVersions.ToDictionary(kv => kv.Key, kv => kv.Value);
        return Task.FromResult<IReadOnlyDictionary<string, int>>(manifest);
    }

    public Task<int> CleanupOldVersionsAsync(string configType, ConfigRetentionOptions retention,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}
