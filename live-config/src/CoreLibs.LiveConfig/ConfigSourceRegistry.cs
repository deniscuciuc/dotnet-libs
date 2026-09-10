using System.Collections.Concurrent;

namespace CoreLibs.LiveConfig;

/// <summary>
/// Registry that maps source names to <see cref="IConfigSource"/> instances.
/// </summary>
public sealed class ConfigSourceRegistry
{
    private readonly ConcurrentDictionary<string, IConfigSource> _sources = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IConfigSource source)
    {
        _sources[source.SourceName] = source;
    }

    public IConfigSource? Resolve(string sourceName)
    {
        return _sources.GetValueOrDefault(sourceName);
    }

    public IReadOnlyCollection<string> SourceNames => _sources.Keys.ToList();

    public IEnumerable<IConfigSource> All => _sources.Values;
}
