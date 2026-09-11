using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.Localization.Runtime;

/// <summary>
/// Main <see cref="ILocalizer"/> implementation.
/// Performs O(1) cache lookup, applies fallback chain, interpolation and pluralization.
/// </summary>
public sealed partial class Localizer : ILocalizer
{
    private readonly ILocalizationCache _cache;
    private readonly LocalizationOptions _options;
    private readonly ILogger<Localizer> _logger;

    public Localizer(
        ILocalizationCache cache,
        IOptions<LocalizationOptions> options,
        ILogger<Localizer> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public string Get(LocalizationKey key, LanguageCode culture, LocalizationArgs args = default)
    {
        return Resolve(key, culture, args);
    }

    public string GetPlural(LocalizationKey key, LanguageCode culture, long count, LocalizationArgs args = default)
    {
        return ResolvePlural(key, culture, count, args);
    }

    private string Resolve(LocalizationKey key, LanguageCode culture, LocalizationArgs args)
    {
        if (TryResolve(key.Key, culture.Value, args, out var result))
            return result!;

        foreach (var fallback in _options.FallbackChain)
        {
            if (fallback == culture.Value) continue;
            if (TryResolve(key.Key, fallback, args, out result))
                return result!;
        }

        if (culture.Value != _options.DefaultCulture
            && !_options.FallbackChain.Contains(_options.DefaultCulture)
            && TryResolve(key.Key, _options.DefaultCulture, args, out result))
            return result!;

        if (_options.LogMissingKeys)
            _logger.LogWarning("Missing localization key '{Key}' for culture '{Culture}'", key.Key, culture.Value);
        return _options.ReturnKeyOnMissing ? key.Key : string.Empty;
    }

    private bool TryResolve(string key, string culture, LocalizationArgs args, out string? result)
    {
        var provider = _cache.GetProvider(culture);
        if (provider is null || !provider.TryGet(key, out var value) || value is null)
        {
            result = null;
            return false;
        }

        result = InterpolationHelper.Interpolate(value.Value, args);
        return true;
    }
}
