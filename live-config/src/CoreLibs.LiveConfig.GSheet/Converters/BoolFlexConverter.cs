using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class BoolFlexConverter : DefaultTypeConverter
{
    private static readonly HashSet<string> TrueValues =
        new(StringComparer.OrdinalIgnoreCase) { "true", "yes", "1", "on" };

    private static readonly HashSet<string> FalseValues =
        new(StringComparer.OrdinalIgnoreCase) { "false", "no", "0", "off" };

    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();

        if (TrueValues.Contains(trimmed)) return true;
        if (FalseValues.Contains(trimmed)) return false;

        throw new TypeConverterException(this, memberMapData, text, row.Context,
            $"Cannot convert '{text}' to bool. Expected: yes/no, true/false, 1/0, on/off");
    }
}
