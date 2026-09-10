namespace CoreLibs.Localization.Runtime;

internal sealed class LocalizationProvider : ILocalizationProvider
{
    private readonly IReadOnlyDictionary<string, LocalizationValue> _entries;

    public LocalizationProvider(string culture, IReadOnlyDictionary<string, LocalizationValue> entries)
    {
        Culture = culture;
        _entries = entries;
    }

    public string Culture { get; }

    public IReadOnlyDictionary<string, LocalizationValue> GetAll()
    {
        return _entries;
    }

    public bool TryGet(string key, out LocalizationValue? value)
    {
        return _entries.TryGetValue(key, out value);
    }
}
