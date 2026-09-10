using System.Collections.Concurrent;
using System.Reflection;
using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Pipeline;

namespace CoreLibs.LiveConfig.GSheet;

public class GSheetImporterRegistry
{
    private readonly HashSet<Type> _types = [];
    private readonly Dictionary<Type, EntityRegistration> _entityRegistrations = [];

    /// <summary>
    /// Static entity registrations — populated during DI configuration, consumed at orchestrator build time.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, EntityRegistration> _staticEntityRegistrations = new();

    /// <summary>
    /// Static entity adapter instances — keyed by adapter type, resolved by the orchestrator.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, object> _staticEntityAdapters = new();

    public IReadOnlyCollection<Type> ImporterTypes => _types;

    /// <summary>
    /// Returns all entity registrations (both instance and static).
    /// </summary>
    internal IReadOnlyCollection<EntityRegistration> EntityRegistrations =>
        _entityRegistrations.Values.Concat(_staticEntityRegistrations.Values).Distinct().ToList();

    public void Add(Type type)
    {
        if (!IsImporterType(type))
            throw new InvalidOperationException($"Type '{type.FullName}' is not an IGSheetImporter<,> implementation.");
        _types.Add(type);
    }

    /// <summary>
    /// Registers an entity adapter with its metadata (static — for use during DI setup).
    /// </summary>
    internal static void RegisterEntityAdapter(EntityRegistration registration, object adapterInstance)
    {
        _staticEntityRegistrations[registration.AdapterType] = registration;
        _staticEntityAdapters[registration.AdapterType] = adapterInstance;
    }

    /// <summary>
    /// Gets a pre-built entity adapter instance, if it exists.
    /// </summary>
    internal static object? GetEntityAdapter(Type adapterType)
    {
        return _staticEntityAdapters.GetValueOrDefault(adapterType);
    }

    /// <summary>
    /// Gets all statically-registered entity registrations.
    /// </summary>
    internal static IReadOnlyCollection<EntityRegistration> GetStaticEntityRegistrations()
    {
        return _staticEntityRegistrations.Values.ToList();
    }

    /// <summary>
    /// Clears all static entity registrations. Used for testing.
    /// </summary>
    internal static void ClearStaticEntityRegistrations()
    {
        _staticEntityRegistrations.Clear();
        _staticEntityAdapters.Clear();
    }

    /// <summary>
    /// Resolves an importer type by a human-readable name.
    /// Matches against the class name (minus "Importer"/"GSheetImporter" suffixes),
    /// the GSheetImporter attribute's SheetName, or the exact class name.
    /// </summary>
    public Type? ResolveByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var normalized = NormalizeName(name);

        foreach (var type in _types)
        {
            if (string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase))
                return type;

            var typeName = NormalizeName(type.Name
                .Replace("GSheetImporter", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Importer", "", StringComparison.OrdinalIgnoreCase));
            if (string.Equals(typeName, normalized, StringComparison.OrdinalIgnoreCase))
                return type;

            var attr = type.GetCustomAttribute<GSheetImporterAttribute>();
            if (attr != null)
            {
                var sheetNormalized = NormalizeName(attr.SheetName);
                if (string.Equals(sheetNormalized, normalized, StringComparison.OrdinalIgnoreCase))
                    return type;
            }
        }

        return null;
    }

    /// <summary>
    /// Same as ResolveByName but searches all discovered importer types.
    /// </summary>
    public static Type? ResolveImporterType(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var normalized = NormalizeName(name);

        foreach (var type in DiscoverImporterTypes())
        {
            var typeName = NormalizeName(type.Name
                .Replace("GSheetImporter", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Importer", "", StringComparison.OrdinalIgnoreCase));

            if (string.Equals(typeName, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase))
                return type;

            var attr = type.GetCustomAttribute<GSheetImporterAttribute>();
            if (attr == null) continue;

            var sheetNormalized = NormalizeName(attr.SheetName);
            if (string.Equals(sheetNormalized, normalized, StringComparison.OrdinalIgnoreCase))
                return type;
        }

        foreach (var reg in GetStaticEntityRegistrations())
        {
            var sheetNormalized = NormalizeName(reg.SheetName);
            if (string.Equals(sheetNormalized, normalized, StringComparison.OrdinalIgnoreCase))
                return reg.AdapterType;
        }

        return null;
    }

    internal static IReadOnlyList<Type> DiscoverImporterAndEntityTypes()
    {
        var importerTypes = DiscoverImporterTypes().ToList();

        foreach (var reg in GetStaticEntityRegistrations())
        {
            if (!importerTypes.Contains(reg.AdapterType))
                importerTypes.Add(reg.AdapterType);
        }

        return importerTypes;
    }

    internal static bool IsImporterType(Type t)
    {
        return t is { IsAbstract: false, IsInterface: false } &&
               !t.ContainsGenericParameters &&
               t.GetInterfaces().Any(i =>
                   i.IsGenericType &&
                   (i.GetGenericTypeDefinition() == typeof(IGSheetImporter<,>) ||
                    i.GetGenericTypeDefinition() == typeof(IGSheetPipeline<,>)));
    }

    /// <summary>
    /// Returns true if the type implements <see cref="IGSheetPipeline{TRow,TDomain}"/>.
    /// </summary>
    internal static bool IsPipelineType(Type t)
    {
        return t.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IGSheetPipeline<,>));
    }

    internal static IEnumerable<Type> DiscoverImporterTypes()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(SafeGetTypes)
            .Where(t => IsImporterType(t) && t.GetCustomAttribute<GSheetImporterAttribute>() is not null);
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try
        {
            return a.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }

    private static string NormalizeName(string value)
    {
        return value.Replace(" ", "").Replace("_", "").Replace("-", "");
    }
}
