using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class DictionaryConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new Dictionary<string, string>();

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in text.Split(','))
        {
            var trimmed = pair.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex < 0)
                throw new TypeConverterException(this, memberMapData, text, row.Context,
                    $"Invalid key:value pair '{trimmed}'. Expected format: key1:val1,key2:val2");

            var key = trimmed[..colonIndex].Trim();
            var value = trimmed[(colonIndex + 1)..].Trim();
            result[key] = value;
        }

        return result;
    }
}
