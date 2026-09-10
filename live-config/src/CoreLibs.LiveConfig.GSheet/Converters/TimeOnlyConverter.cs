using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class TimeOnlyConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        return TimeOnly.TryParse(text, out var result)
            ? result
            : throw new TypeConverterException(this, memberMapData, text, row.Context,
                $"Invalid TimeOnly value: {text}");
    }
}
