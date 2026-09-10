using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.GSheet.Converters;

public class JsonCellConverter<T> : DefaultTypeConverter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return default(T);

        try
        {
            return JsonSerializer.Deserialize<T>(text, Options);
        }
        catch (JsonException ex)
        {
            throw new TypeConverterException(this, memberMapData, text, row.Context,
                $"Failed to parse JSON for type {typeof(T).Name}: {ex.Message}");
        }
    }
}
