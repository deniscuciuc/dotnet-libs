using CsvHelper.Configuration;

namespace CoreLibs.Localization.LiveConfig.GSheet;

/// <summary>
/// CsvHelper mapper for <see cref="LocalizationGSheetRow"/>.
/// Adapted from CoreLibs.Localization.GSheet.GSheetLocalizedTextMapper.
/// </summary>
internal sealed class LocalizationGSheetMapper : ClassMap<LocalizationGSheetRow>
{
    public static readonly LocalizationGSheetMapper Default = new();

    public LocalizationGSheetMapper()
    {
        Map(m => m.Key).Name("Key");
        Map(m => m.Translations).Convert(row =>
        {
            var dict = new Dictionary<string, string>();
            foreach (var header in row.Row.HeaderRecord!)
            {
                if (header == "Key") continue;
                var value = row.Row.GetField(header);
                if (!string.IsNullOrWhiteSpace(value))
                    dict[header] = value;
            }

            return dict;
        });
    }
}
