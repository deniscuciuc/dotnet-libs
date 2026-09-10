using System.Reflection;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace CoreLibs.LiveConfig.GSheet.Schema;

/// <summary>
/// Builds a <see cref="ClassMap{T}"/> automatically from <see cref="GSheetColumnAttribute"/>,
/// <see cref="GSheetIgnoreAttribute"/>, and <see cref="GSheetDefaultAttribute"/> annotations
/// on <typeparamref name="TRow"/> properties.
/// <para>
/// Convention: if no <see cref="GSheetColumnAttribute"/> attributes are found on any property,
/// all public readable properties are auto-mapped by their name (convention-based fallback).
/// </para>
/// </summary>
public static class AttributeClassMapBuilder
{
    /// <summary>
    /// Builds a <see cref="ClassMap{TRow}"/> for the given row type using attribute metadata.
    /// </summary>
    public static ClassMap<TRow> Build<TRow>()
    {
        var map = new DynamicClassMap<TRow>();
        var properties = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        var hasColumnAttributes = properties.Any(p => p.GetCustomAttribute<GSheetColumnAttribute>() != null);

        foreach (var prop in properties)
        {
            if (prop.GetCustomAttribute<GSheetIgnoreAttribute>() != null)
                continue;

            var colAttr = prop.GetCustomAttribute<GSheetColumnAttribute>();
            var defaultAttr = prop.GetCustomAttribute<GSheetDefaultAttribute>();

            // If no [GSheetColumn] attributes anywhere → convention: map by property name
            // If [GSheetColumn] attributes exist → only map annotated properties
            if (hasColumnAttributes && colAttr is null)
                continue;

            var memberMap = map.Map(typeof(TRow), prop);
            var columnName = colAttr?.Name ?? prop.Name;
            memberMap.Data.Names.Clear();
            memberMap.Data.Names.Add(columnName);
            memberMap.Data.NameIndex = 0;
            memberMap.Data.IsNameSet = true;

            if (colAttr is { Order: >= 0 })
                memberMap.Data.Index = colAttr.Order;

            if (defaultAttr is not null)
                memberMap.Data.Default = defaultAttr.Value is null
                    ? null
                    : new DefaultAttribute(defaultAttr.Value).Default;
        }

        // For record types (no parameterless constructor), add constructor parameter mappings
        // so CsvHelper can instantiate via the primary constructor.
        var hasParameterlessCtor = typeof(TRow).GetConstructor(Type.EmptyTypes) is not null;
        if (hasParameterlessCtor) return map;

        var ctor = typeof(TRow).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (ctor is null) return map;

        foreach (var param in ctor.GetParameters())
        {
            var matchingProp = properties.FirstOrDefault(p =>
                string.Equals(p.Name, param.Name, StringComparison.OrdinalIgnoreCase));

            if (matchingProp is null || param.Name is null) continue;

            var colAttr = matchingProp.GetCustomAttribute<GSheetColumnAttribute>();
            if (hasColumnAttributes && colAttr is null) continue;

            var columnName = colAttr?.Name ?? matchingProp.Name;
            var paramMap = map.Parameter(param.Name);
            paramMap.Data.Names.Clear();
            paramMap.Data.Names.Add(columnName);
            paramMap.Data.IsNameSet = true;
        }

        return map;
    }

    /// <summary>
    /// Extracts the expected column names for <typeparamref name="TRow"/> from attribute metadata.
    /// Used by structural validation to check sheet headers before parsing.
    /// </summary>
    public static IReadOnlyList<string> GetExpectedColumns<TRow>()
    {
        return GetExpectedColumns(typeof(TRow));
    }

    /// <summary>
    /// Extracts the expected column names for the given row type from attribute metadata.
    /// </summary>
    public static IReadOnlyList<string> GetExpectedColumns(Type rowType)
    {
        var properties = rowType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        var hasColumnAttributes = properties.Any(p => p.GetCustomAttribute<GSheetColumnAttribute>() != null);

        return (from prop in properties
                where prop.GetCustomAttribute<GSheetIgnoreAttribute>() == null
                let colAttr = prop.GetCustomAttribute<GSheetColumnAttribute>()
                where !hasColumnAttributes || colAttr is not null
                select colAttr?.Name ?? prop.Name).ToList();
    }

    /// <summary>
    /// Internal ClassMap implementation that allows dynamic member mapping.
    /// </summary>
    private sealed class DynamicClassMap<T> : ClassMap<T>;
}
