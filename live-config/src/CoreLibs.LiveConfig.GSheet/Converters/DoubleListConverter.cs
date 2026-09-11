using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class DoubleListConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<double>();
        return text.Split(", ")
            .Select(s => s.Trim())
            .Where(s => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
            .ToList();
    }
}
