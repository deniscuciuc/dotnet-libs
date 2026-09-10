using System.Collections;
using System.Reflection;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Resolves <see cref="GSheetRefAttribute"/> annotations on row properties by looking up
/// referenced domain data in the <see cref="GSheetImportContext"/>. Validates that every
/// referenced value exists, producing clear errors when it doesn't.
/// </summary>
public static class ReferenceResolver
{
    /// <summary>
    /// Validates all <see cref="GSheetRefAttribute"/> references on the row type.
    /// Returns errors for any reference values not found in the upstream domain data.
    /// </summary>
    public static GSheetValidationResult Validate<TRow>(
        IReadOnlyList<TRow> rows,
        GSheetImportContext context,
        string sheetName)
    {
        var refProps = GetReferenceProperties(typeof(TRow));
        if (refProps.Count == 0)
            return GSheetValidationResult.Ok();

        var errors = new List<GSheetValidationError>();

        foreach (var refProp in refProps)
        {
            var lookupSet = BuildLookupSet(refProp.Attribute, context);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row is null) continue;

                var value = refProp.Property.GetValue(row);

                var key = value?.ToString();
                if (key is null || lookupSet.Contains(key)) continue;

                var colAttr = refProp.Property.GetCustomAttribute<GSheetColumnAttribute>();
                var fieldName = colAttr?.Name ?? refProp.Property.Name;

                errors.Add(new GSheetValidationError(
                    i,
                    fieldName,
                    $"Reference '{key}' not found in {refProp.Attribute.ReferencedDomainType.Name}" +
                    $".{refProp.Attribute.KeyProperty} (sheet '{sheetName}', row {i + 2})"));
            }
        }

        return errors.Count == 0
            ? GSheetValidationResult.Ok()
            : GSheetValidationResult.Fail(errors);
    }

    /// <summary>
    /// Extracts the domain types that the row type references via <see cref="GSheetRefAttribute"/>.
    /// Used by the orchestrator to auto-infer dependency ordering.
    /// </summary>
    public static IReadOnlyList<Type> GetReferencedDomainTypes(Type rowType)
    {
        return GetReferenceProperties(rowType)
            .Select(r => r.Attribute.ReferencedDomainType)
            .Distinct()
            .ToList();
    }

    private static HashSet<string> BuildLookupSet(GSheetRefAttribute attr, GSheetImportContext context)
    {
        var domainData = context.GetDomain(attr.ReferencedDomainType);
        if (domainData is null)
            return [];

        var keyProp =
            attr.ReferencedDomainType.GetProperty(attr.KeyProperty, BindingFlags.Public | BindingFlags.Instance);
        if (keyProp is null)
            return [];

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (domainData is not IEnumerable enumerable) return set;

        foreach (var item in enumerable)
        {
            var keyValue = keyProp.GetValue(item)?.ToString();
            if (keyValue is not null)
                set.Add(keyValue);
        }

        return set;
    }

    private static List<(PropertyInfo Property, GSheetRefAttribute Attribute)> GetReferenceProperties(Type rowType)
    {
        return rowType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => (Property: p, Attr: p.GetCustomAttribute<GSheetRefAttribute>()))
            .Where(x => x.Attr is not null)
            .Select(x => (x.Property, x.Attr!))
            .ToList();
    }
}
