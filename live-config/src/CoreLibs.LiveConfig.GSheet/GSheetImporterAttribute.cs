using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

namespace CoreLibs.LiveConfig.GSheet;

[AttributeUsage(AttributeTargets.Class)]
public partial class GSheetImporterAttribute : Attribute
{
    [GeneratedRegex(
        @"^\s*[A-Z]+(?:[0-9]+)?(?:\s*:\s*[A-Z]+(?:[0-9]+)?)?\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex A1RangeRegex();

    public GSheetImporterAttribute(string sheetName, string range = "")
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

    /// <summary>
    ///     Types of importers or pipelines that must complete before this importer runs.
    /// </summary>
    public Type[] Needs
    {
        get;
        set
        {
            Contract.Assert(value != null);
            foreach (var type in value)
                if (!ImplementsGSheetDependency(type))
                    throw new ArgumentException(
                        $"Type '{type.FullName}' in Needs must implement IGSheetImporter<,> or IGSheetPipeline<,>.");

            field = value;
        }
    } = [];

    private static bool ImplementsGSheetDependency(Type type)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType &&
                      (i.GetGenericTypeDefinition() == typeof(IGSheetImporter<,>) ||
                       i.GetGenericTypeDefinition() == typeof(Pipeline.IGSheetPipeline<,>)));
    }
}
