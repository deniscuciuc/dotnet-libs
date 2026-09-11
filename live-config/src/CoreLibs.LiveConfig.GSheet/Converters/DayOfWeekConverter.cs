using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class DayOfWeekConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        return Enum.TryParse<DayOfWeek>(text, true, out var result)
            ? result
            : throw new TypeConverterException(this, memberMapData, text, row.Context,
                $"Invalid DayOfWeek value: {text}");
    }
}
