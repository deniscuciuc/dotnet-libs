namespace CoreLibs.Localization.LiveConfig.GSheet;

/// <summary>
/// Converts parsed GSheet rows into <see cref="LocalizationSnapshot"/> objects grouped by culture.
/// </summary>
public static class LocalizationGSheetConverter
{
    public static IEnumerable<LocalizationSnapshot> Convert(
        IReadOnlyList<LocalizationGSheetRow> rows)
    {
        var cultures = rows
            .SelectMany(r => r.Translations.Keys)
            .Distinct()
            .ToList();
        foreach (var culture in cultures)
        {
            var entries = new Dictionary<string, LocalizationValue>();
            foreach (var row in rows)
            {
                if (!row.Translations.TryGetValue(culture, out var text))
                    continue;
                entries[row.Key] = new LocalizationValue { Value = text };
            }

            yield return new LocalizationSnapshot
            {
                Culture = culture,
                Entries = entries
            };
        }
    }
}
