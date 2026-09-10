using System.Globalization;
using Microsoft.Extensions.Logging;

namespace CoreLibs.Localization.Runtime;

public sealed partial class Localizer
{
    internal string ResolvePlural(LocalizationKey key, LanguageCode culture, long count, LocalizationArgs args)
    {
        foreach (var c in BuildFallbackList(culture))
        {
            var provider = _cache.GetProvider(c);
            if (provider is null || !provider.TryGet(key.Key, out var value) || value is null)
                continue;

            // Inject count into args so templates can use {count}; caller args are preserved.
            var enriched = args.And("count", count.ToString(CultureInfo.InvariantCulture));

            if (value.Plurals is { Count: > 0 })
            {
                var form = PluralizationHelper.SelectForm(value.Plurals, count, c, _options.PluralRules);
                if (form is not null)
                    return InterpolationHelper.Interpolate(form, enriched);
            }

            return InterpolationHelper.Interpolate(value.Value, enriched);
        }

        if (_options.LogMissingKeys)
            _logger.LogWarning("Missing plural key '{Key}' for culture '{Culture}'", key.Key, culture.Value);
        return _options.ReturnKeyOnMissing ? key.Key : string.Empty;
    }

    private IEnumerable<string> BuildFallbackList(LanguageCode culture)
    {
        yield return culture.Value;
        foreach (var f in _options.FallbackChain)
            if (f != culture.Value)
                yield return f;

        if (culture.Value != _options.DefaultCulture
            && !_options.FallbackChain.Contains(_options.DefaultCulture))
            yield return _options.DefaultCulture;
    }
}
