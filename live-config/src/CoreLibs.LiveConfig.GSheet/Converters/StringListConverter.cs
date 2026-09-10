using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class StringListConverter<TSeparator> : DefaultTypeConverter where TSeparator : ISeparator, new()
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        var separator = new TSeparator();
        var sep = separator.Separator;

        var parts = sep.Length == 1 ? text.Split(sep[0]) : text.Split([sep], StringSplitOptions.None);

        return parts
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
}
