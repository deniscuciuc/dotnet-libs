using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class DecimalListConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<decimal>();
        return text.Split(", ")
            .Select(s => s.Trim())
            .Where(s => decimal.TryParse(s, out _))
            .Select(decimal.Parse)
            .ToList();
    }
}
