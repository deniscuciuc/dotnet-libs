namespace CoreLibs.Localization;

public interface ILocalizationStore
{
    Task<IEnumerable<LocalizationEntry>> FindAllAsync(string culture, CancellationToken ct = default);

    Task UpsertAsync(LocalizationEntry entry, CancellationToken ct = default);

    Task DeleteAsync(string key, string culture, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, string culture, CancellationToken ct = default);

    Task EnsureCreatedAsync(CancellationToken ct = default);

    Task UpsertManyAsync(IEnumerable<LocalizationEntry> entries, CancellationToken ct = default)
    {
        return Task.WhenAll(entries.Select(e => UpsertAsync(e, ct)));
    }
}
