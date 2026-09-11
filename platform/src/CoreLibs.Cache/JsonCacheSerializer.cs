using System.Text.Json;

namespace CoreLibs.Cache;

public sealed class JsonCacheSerializer : ICacheSerializer
{
    public byte[] Serialize<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value);

    public T? Deserialize<T>(byte[] data) =>
        JsonSerializer.Deserialize<T>(data);
}
