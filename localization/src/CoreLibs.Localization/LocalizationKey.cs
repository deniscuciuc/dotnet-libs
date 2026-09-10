namespace CoreLibs.Localization;

/// <summary>
/// Strongly-typed localization key (e.g. "offers.welcome.title").
/// </summary>
public readonly struct LocalizationKey : IEquatable<LocalizationKey>
{
    public string Key { get; }

    public LocalizationKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Localization key cannot be null or whitespace.", nameof(key));
        Key = key;
    }

    public override string ToString()
    {
        return Key;
    }

    public static implicit operator string(LocalizationKey k)
    {
        return k.Key;
    }

    public static implicit operator LocalizationKey(string value)
    {
        return new LocalizationKey(value);
    }

    public bool Equals(LocalizationKey other)
    {
        return Key == other.Key;
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            string s => Key.Equals(s, StringComparison.Ordinal),
            LocalizationKey other => Equals(other),
            _ => false
        };
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}
