using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class NullableConverter<TConverter> : DefaultTypeConverter where TConverter : DefaultTypeConverter, new()
{
    private readonly TConverter _inner = new();

    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return _inner.ConvertFromString(text, row, memberMapData);
    }
}
