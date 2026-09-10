using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Produces deterministic JSON output by sorting properties alphabetically
/// and using consistent formatting. This ensures that identical domain data
/// always produces the same JSON string, making content hashing reliable.
/// </summary>
public static class CanonicalJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Serialize(object value, Type type)
    {
        var json = JsonSerializer.Serialize(value, type, Options);

        // Re-parse and sort for deterministic key ordering
        using var doc = JsonDocument.Parse(json);
        return SerializeElement(doc.RootElement);
    }

    public static string Serialize<T>(T value)
    {
        return Serialize(value!, typeof(T));
    }

    private static string SerializeElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var properties = element.EnumerateObject()
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .Select(p => $"{JsonSerializer.Serialize(p.Name)}:{SerializeElement(p.Value)}");
                return $"{{{string.Join(",", properties)}}}";

            case JsonValueKind.Array:
                var items = element.EnumerateArray()
                    .Select(SerializeElement);
                return $"[{string.Join(",", items)}]";

            case JsonValueKind.Undefined:
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
            default:
                return element.GetRawText();
        }
    }
}
