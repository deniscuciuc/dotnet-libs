using System.Diagnostics.CodeAnalysis;

namespace CoreLibs.Localization;

/// <summary>
/// Strongly-typed, immutable bag of named string values used for template interpolation.
/// Build via <see cref="With"/> + <see cref="And"/>, or implicitly from a <see cref="Dictionary{TKey,TValue}"/>.
/// </summary>
public readonly struct LocalizationArgs
{
    private readonly IReadOnlyDictionary<string, string>? _values;

    private LocalizationArgs(IReadOnlyDictionary<string, string> values) => _values = values;

    /// <summary>Gets a value indicating whether the args bag contains no entries.</summary>
    public bool IsEmpty => _values is null || _values.Count == 0;

    /// <summary>Creates a new args bag with a single named entry.</summary>
    public static LocalizationArgs With(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new LocalizationArgs(new Dictionary<string, string>(1, StringComparer.OrdinalIgnoreCase)
        { [key] = value });
    }

    /// <summary>
    /// Returns a new args bag with an additional entry appended.
    /// If <paramref name="key"/> already exists its value is overwritten.
    /// </summary>
    public LocalizationArgs And(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        var capacity = (_values?.Count ?? 0) + 1;
        var dict = new Dictionary<string, string>(capacity, StringComparer.OrdinalIgnoreCase);
        if (_values is not null)
            foreach (var (k, v) in _values)
                dict[k] = v;
        dict[key] = value;
        return new LocalizationArgs(dict);
    }

    /// <summary>
    /// Implicitly wraps a pre-built <see cref="Dictionary{TKey,TValue}"/> as args.
    /// Useful when building args from a query string or other runtime sources.
    /// </summary>
    public static implicit operator LocalizationArgs(Dictionary<string, string>? args)
        => args is null ? default : new LocalizationArgs(args);

    /// <summary>Tries to get the string value for the given placeholder name.</summary>
    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        if (_values is not null) return _values.TryGetValue(key, out value);

        value = null;
        return false;
    }
}
