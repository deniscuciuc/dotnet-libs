using System.Reflection;
using System.Text.RegularExpressions;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.GSheet.Validation;

/// <summary>
/// Validates parsed row instances against <see cref="GSheetRequiredAttribute"/>,
/// <see cref="GSheetRangeAttribute"/>, and <see cref="GSheetRegexAttribute"/> annotations.
/// Runs automatically after CsvHelper parsing in the pipeline.
/// </summary>
public static class AttributeRowValidator
{
    /// <summary>
    /// Validates all rows and collects errors from attribute-based rules.
    /// </summary>
    public static GSheetValidationResult Validate<TRow>(IReadOnlyList<TRow> rows)
    {
        var rules = BuildRules(typeof(TRow));
        if (rules.Count == 0)
            return GSheetValidationResult.Ok();

        var errors = new List<GSheetValidationError>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row is null) continue;

            foreach (var rule in rules)
            {
                var value = rule.Property.GetValue(row);
                var fieldName = rule.FieldName;

                // [GSheetRequired]
                if (rule.IsRequired && IsEmpty(value))
                {
                    errors.Add(new GSheetValidationError(i, fieldName, $"'{fieldName}' is required"));
                    continue; // skip further checks for this property
                }

                if (value is null)
                    continue;

                // [GSheetRange]
                if (rule.Range is not null)
                {
                    var numeric = ConvertToDouble(value);
                    if (numeric.HasValue)
                        if (numeric.Value < rule.Range.Value.Min || numeric.Value > rule.Range.Value.Max)
                            errors.Add(new GSheetValidationError(i, fieldName,
                                $"'{fieldName}' value {numeric.Value} is outside range [{rule.Range.Value.Min}, {rule.Range.Value.Max}]"));
                }

                // [GSheetRegex]
                if (rule.RegexPattern is null) continue;

                var str = value.ToString();
                if (str is not null && !rule.CompiledRegex!.IsMatch(str))
                    errors.Add(new GSheetValidationError(i, fieldName,
                        $"'{fieldName}' value '{str}' does not match pattern '{rule.RegexPattern}'"));
            }
        }

        return errors.Count == 0
            ? GSheetValidationResult.Ok()
            : GSheetValidationResult.Fail(errors);
    }

    private static bool IsEmpty(object? value)
    {
        return value switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            _ => value.Equals(GetDefault(value.GetType()))
        };
    }

    private static object? GetDefault(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static double? ConvertToDouble(object value)
    {
        try
        {
            return Convert.ToDouble(value);
        }
        catch
        {
            return null;
        }
    }

    private static List<PropertyRule> BuildRules(Type rowType)
    {
        var properties = rowType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        return (from prop in properties
                let required = prop.GetCustomAttribute<GSheetRequiredAttribute>()
                let range = prop.GetCustomAttribute<GSheetRangeAttribute>()
                let regex = prop.GetCustomAttribute<GSheetRegexAttribute>()
                where required is not null || range is not null || regex is not null
                let colAttr = prop.GetCustomAttribute<GSheetColumnAttribute>()
                let fieldName = colAttr?.Name ?? prop.Name
                select new PropertyRule
                {
                    Property = prop,
                    FieldName = fieldName,
                    IsRequired = required is not null,
                    Range = range is not null ? (range.Minimum, range.Maximum) : null,
                    RegexPattern = regex?.Pattern,
                    CompiledRegex = regex is not null ? new Regex(regex.Pattern, RegexOptions.Compiled) : null
                }).ToList();
    }

    private sealed class PropertyRule
    {
        public required PropertyInfo Property { get; init; }
        public required string FieldName { get; init; }
        public bool IsRequired { get; init; }
        public (double Min, double Max)? Range { get; init; }
        public string? RegexPattern { get; init; }
        public Regex? CompiledRegex { get; init; }
    }
}
