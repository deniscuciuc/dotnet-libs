using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class IntListConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<int>();
        return text.Split(", ")
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out _))
            .Select(int.Parse)
            .ToList();
    }
}
