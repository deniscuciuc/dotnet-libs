using System.Collections.Immutable;

namespace CoreLibs.Localization.Runtime;

/// <summary>
/// Thread-safe in-memory localization cache.
/// All reads are lock-free; writes perform an atomic Interlocked.Exchange.
/// </summary>
public sealed class LocalizationCache : ILocalizationCache
{
    private volatile ImmutableDictionary<string, ILocalizationProvider> _snapshot
        = ImmutableDictionary<string, ILocalizationProvider>.Empty;

    public ILocalizationProvider? GetProvider(string culture)
    {
        return _snapshot.TryGetValue(culture, out var p) ? p : null;
    }

    public void Update(string culture, IReadOnlyDictionary<string, LocalizationValue> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);
        ArgumentNullException.ThrowIfNull(entries);

        ImmutableDictionary<string, ILocalizationProvider> before, after;
        do
        {
            before = _snapshot;
            after = before.SetItem(culture, new LocalizationProvider(culture, entries));
        } while (!ReferenceEquals(
                     Interlocked.CompareExchange(ref _snapshot, after, before), before));
    }

    public IReadOnlyList<string> GetLoadedCultures()
    {
        return _snapshot.Keys.ToList();
    }
}
