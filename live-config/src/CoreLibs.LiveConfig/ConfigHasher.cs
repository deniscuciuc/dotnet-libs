using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CoreLibs.LiveConfig;

/// <summary>
/// Utilities for deterministic hashing and JSON serialization of config data.
/// Uses pooled buffers to avoid LOH allocations for large configs.
/// </summary>
public static class ConfigHasher
{
    /// <summary>
    /// Computes an SHA-256 hash of the JSON string.
    /// Uses ArrayPool for configs larger than 1KB to reduce GC pressure.
    /// </summary>
    public static string ComputeHash(string json)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(json.Length);

        if (maxByteCount <= 1024)
        {
            Span<byte> buffer = stackalloc byte[maxByteCount];
            var bytesWritten = Encoding.UTF8.GetBytes(json, buffer);
            Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(buffer[..bytesWritten], hash);
            return Convert.ToHexStringLower(hash);
        }

        var rentedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
        try
        {
            var bytesWritten = Encoding.UTF8.GetBytes(json, rentedBuffer);
            Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(rentedBuffer.AsSpan(0, bytesWritten), hash);
            return Convert.ToHexStringLower(hash);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    /// <summary>
    /// Serializes the data to JSON and computes a SHA-256 hash.
    /// </summary>
    public static string ComputeHash<T>(T data)
    {
        var json = SerializeToJson(data);
        return ComputeHash(json);
    }

    /// <summary>
    /// Serializes data to JSON using the shared config serializer options.
    /// </summary>
    public static string SerializeToJson<T>(T data)
    {
        return JsonSerializer.Serialize(data, ConfigJsonOptions.Default);
    }

    /// <summary>
    /// Deserializes JSON to the specified type using the shared config serializer options.
    /// </summary>
    public static T? DeserializeFromJson<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, ConfigJsonOptions.Default);
    }
}
