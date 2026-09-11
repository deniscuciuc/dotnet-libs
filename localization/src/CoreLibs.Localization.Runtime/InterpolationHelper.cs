using System.Text.RegularExpressions;

namespace CoreLibs.Localization.Runtime;

/// <summary>
/// Replaces named placeholders <c>{key}</c> in a template string using a <see cref="LocalizationArgs"/> bag.
/// </summary>
internal static partial class InterpolationHelper
{
    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    public static string Interpolate(string template, LocalizationArgs args)
    {
        if (args.IsEmpty || !template.Contains('{'))
            return template;

        return PlaceholderRegex().Replace(template, m =>
        {
            var name = m.Groups[1].Value;
            return args.TryGetValue(name, out var v) ? v : m.Value;
        });
    }
}
