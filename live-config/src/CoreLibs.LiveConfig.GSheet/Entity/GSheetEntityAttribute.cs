using System.Text.RegularExpressions;

namespace CoreLibs.LiveConfig.GSheet.Entity;

/// <summary>
/// Marks a domain model as a GSheet entity — a declarative, attribute-driven alternative
/// to implementing <see cref="Pipeline.IGSheetPipeline{TRow,TDomain}"/> directly.
/// <para>
/// For simple 1:1 entities, this attribute alone is sufficient (Tier 1).
/// For custom validation or ordering, pair with a <see cref="GSheetEntityConfig{TDomain}"/> class (Tier 2).
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public partial class GSheetEntityAttribute : Attribute
{
    [GeneratedRegex(
        @"^\s*[A-Z]+(?:[0-9]+)?(?:\s*:\s*[A-Z]+(?:[0-9]+)?)?\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex A1RangeRegex();

    public GSheetEntityAttribute(string sheetName, string range = "")
    {
        if (string.IsNullOrWhiteSpace(sheetName))
            throw new ArgumentException("Sheet name cannot be null or whitespace.", nameof(sheetName));

        SheetName = sheetName.Trim();

        if (!string.IsNullOrWhiteSpace(range))
        {
            if (!A1RangeRegex().IsMatch(range.Trim()))
                throw new ArgumentException(
                    "Range must be an A1-style reference (e.g., A1, A:Y, A1:Y, A:Y50, A1:C100).",
                    nameof(range));
            Range = range.Trim();
        }
        else
        {
            Range = string.Empty;
        }
    }

    public string SheetName { get; }
    public string Range { get; }
}
