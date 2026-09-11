using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class EnumConverter<T> : DefaultTypeConverter where T : struct, Enum
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (Enum.TryParse<T>(text, true, out var value))
            return value;
        throw new TypeConverterException(this, memberMapData, text, row.Context,
            $"Cannot convert '{text}' to enum {typeof(T).Name}");
    }
}
