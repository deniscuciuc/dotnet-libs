using System.Collections;
using System.Security.Claims;
using System.Text;

namespace CoreLibs.Identity.Identity;

public readonly struct IdentityClaims(IReadOnlyDictionary<string, string> dictionary)
    : IReadOnlyDictionary<string, string>, IEquatable<IdentityClaims>
{
    public static readonly IdentityClaims None = new();

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

    private readonly IReadOnlyDictionary<string, string> _dictionary = new Dictionary<string, string>(dictionary);

    public IdentityClaims(IEnumerable<Claim> claims)
        : this(claims.ToDictionary(x => x.Type, x => x.Value))
    {
    }

    public int Count => Dictionary.Count;

    public IEnumerable<string> Keys => Dictionary.Keys;

    public IEnumerable<string> Values => Dictionary.Values;

    public string this[string key] => Dictionary[key];

    private IReadOnlyDictionary<string, string> Dictionary => _dictionary ?? Empty;

    public static bool operator ==(IdentityClaims x, IdentityClaims y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(IdentityClaims x, IdentityClaims y)
    {
        return !(x == y);
    }

    public static bool TryParse(string value, out IdentityClaims claims)
    {
        claims = None;

        var dictionary = new Dictionary<string, string>();

        var pairs = value.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var segments = pair.Split('=', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 2) return false;

            if (!dictionary.TryAdd(segments[0], segments[1])) return false;
        }

        claims = new IdentityClaims(dictionary);

        return true;
    }

    public bool ContainsKey(string key)
    {
        return Dictionary.ContainsKey(key);
    }

    public bool TryGetValue(string key, out string value)
    {
        return Dictionary.TryGetValue(key, out value!);
    }

    public bool Equals(IdentityClaims other)
    {
        if (Dictionary.Count != other.Dictionary.Count) return false;

        foreach (var pair in Dictionary)
        {
            if (!other.Dictionary.TryGetValue(pair.Key, out var value)) return false;

            if (pair.Value != value) return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is IdentityClaims claims && Equals(claims);
    }

    public override int GetHashCode()
    {
        return Dictionary.Aggregate(0,
            (current, pair) => HashCode.Combine(current, pair.Key.GetHashCode(), pair.Value.GetHashCode()));
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        foreach (var pair in Dictionary) sb.Append($"{pair.Key}={pair.Value};");

        return sb.ToString();
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return Dictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
