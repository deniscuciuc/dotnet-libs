namespace CoreLibs.Localization;

/// <summary>
/// Strongly-typed ISO 639-1 two-letter language code (e.g. "ru", "en").
/// </summary>
public readonly struct LanguageCode : IEquatable<LanguageCode>
{
    public string Value { get; }

    public LanguageCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Language code cannot be null or whitespace.", nameof(value));
        if (value.Length != 2)
            throw new ArgumentException("Language code must be a 2-letter ISO 639-1 code.", nameof(value));
        if (!char.IsLetter(value[0]) || !char.IsLetter(value[1]))
            throw new ArgumentException("Language code must contain only letters.", nameof(value));
        Value = value.ToLowerInvariant();
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(LanguageCode code)
    {
        return code.Value;
    }

    public static implicit operator LanguageCode(string value)
    {
        return new LanguageCode(value);
    }

    public static LanguageCode FromCulture(string culture)
    {
        return culture.Split('-')[0];
    }

    public bool Equals(LanguageCode other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is LanguageCode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
