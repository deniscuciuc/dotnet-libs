using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class GuidConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        return Guid.TryParse(text, out var result)
            ? result
            : throw new TypeConverterException(this, memberMapData, text, row.Context,
                $"Invalid GUID value: '{text}'");
    }
}
