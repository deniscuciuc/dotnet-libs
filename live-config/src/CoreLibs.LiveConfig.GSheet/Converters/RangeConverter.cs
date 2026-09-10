using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class RangeConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new TypeConverterException(this, memberMapData, text, row.Context,
                "Range value cannot be empty. Expected format: min-max (e.g. 1-10)");

        var dashIndex = text.IndexOf('-', text.StartsWith("-") ? 1 : 0);
        if (dashIndex < 0)
            throw new TypeConverterException(this, memberMapData, text, row.Context,
                $"Invalid range '{text}'. Expected format: min-max (e.g. 1-10)");

        var minText = text[..dashIndex].Trim();
        var maxText = text[(dashIndex + 1)..].Trim();

        if (!double.TryParse(minText, NumberStyles.Float, CultureInfo.InvariantCulture, out var min) || !double.TryParse(maxText, NumberStyles.Float, CultureInfo.InvariantCulture, out var max))
            throw new TypeConverterException(this, memberMapData, text, row.Context,
                $"Invalid range '{text}'. Both min and max must be numeric.");

        return new NumericRange(min, max);
    }
}
