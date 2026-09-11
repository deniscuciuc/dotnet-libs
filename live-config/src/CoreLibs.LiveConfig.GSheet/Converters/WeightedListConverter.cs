using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class WeightedListConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<WeightedItem<string>>();

        var result = new List<WeightedItem<string>>();

        foreach (var pair in text.Split(','))
        {
            var trimmed = pair.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex < 0)
                throw new TypeConverterException(this, memberMapData, text, row.Context,
                    $"Invalid weighted item '{trimmed}'. Expected format: key:weight (e.g. gold:50)");

            var key = trimmed[..colonIndex].Trim();
            var weightText = trimmed[(colonIndex + 1)..].Trim();

            if (!double.TryParse(weightText, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight))
                throw new TypeConverterException(this, memberMapData, text, row.Context,
                    $"Invalid weight '{weightText}' for key '{key}'. Weight must be numeric.");

            result.Add(new WeightedItem<string>(key, weight));
        }

        return result;
    }
}
