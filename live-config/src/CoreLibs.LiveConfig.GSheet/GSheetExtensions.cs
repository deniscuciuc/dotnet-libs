using System.Reflection;
using System.Text;
using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.LiveConfig.GSheet;

public static class GSheetExtensions
{
    public static IServiceCollection AddLiveConfigGSheet(
        this IServiceCollection services,
        Action<GSheetOptions> configure)
    {
        var options = new GSheetOptions();
        configure(options);
        services.AddSingleton(options);
        RegisterCoreServices(services);
        return services;
    }

    public static IServiceCollection AddLiveConfigGSheet(
        this IServiceCollection services,
        IConfiguration configuration,
        string section = GSheetOptions.SectionPath)
    {
        var options = configuration.GetSection(section).Get<GSheetOptions>() ?? new GSheetOptions();
        services.AddSingleton(options);
        RegisterCoreServices(services);
        return services;
    }

    public static IServiceCollection AddGSheetImportersInAssemblyOf<T>(this IServiceCollection services)
    {
        var importerInterface = typeof(IGSheetImporter<,>);
        var pipelineInterface = typeof(IGSheetPipeline<,>);
        var assembly = typeof(T).Assembly;
        var types = SafeGetTypes(assembly)
            .Where(t => t is { IsAbstract: false, IsInterface: false } &&
                        t.GetInterfaces().Any(i =>
                            i.IsGenericType &&
                            (i.GetGenericTypeDefinition() == importerInterface ||
                             i.GetGenericTypeDefinition() == pipelineInterface)));

        foreach (var t in types) services.AddSingleton(t);
        return services;
    }

    public static IServiceCollection AddGSheetPipelineObserver<TObserver>(this IServiceCollection services)
        where TObserver : class, IGSheetPipelineObserver
    {
        services.AddSingleton<IGSheetPipelineObserver, TObserver>();
        return services;
    }

    public static IServiceCollection AddGSheetImporter<TImporter>(this IServiceCollection services)
    {
        var type = typeof(TImporter);
        if (!GSheetImporterRegistry.IsImporterType(type))
            throw new InvalidOperationException($"Type '{type.FullName}' is not an IGSheetImporter<,> implementation.");
        services.AddSingleton(type);
        return services;
    }

    /// <summary>
    /// Discovers and registers GSheet entities from the assembly containing <typeparamref name="T"/>.
    /// Finds: (1) types with [GSheetEntity] (Tier 1/2), (2) GSheetEntityConfig subclasses (Tier 2/3).
    /// Entity adapters are created eagerly and registered as pipeline implementations.
    /// </summary>
    public static IServiceCollection AddGSheetEntitiesInAssemblyOf<T>(this IServiceCollection services)
    {
        var assembly = typeof(T).Assembly;
        var types = SafeGetTypes(assembly).ToList();

        var singleGenericConfigBase = typeof(GSheetEntityConfig<>);
        var dualGenericConfigBase = typeof(GSheetEntityConfig<,>);

        // Track which domain types have a config class
        var configuredDomainTypes = new HashSet<Type>();

        // ── Tier 2/3: GSheetEntityConfig<TDomain> and GSheetEntityConfig<TRow, TDomain> ──
        foreach (var configType in types.Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            var baseType = configType.BaseType;
            if (baseType is null || !baseType.IsGenericType)
                continue;

            var genericDef = baseType.GetGenericTypeDefinition();
            var genericArgs = baseType.GetGenericArguments();

            if (genericDef == singleGenericConfigBase)
            {
                // GSheetEntityConfig<TDomain> — domain = row
                var domainType = genericArgs[0];
                configuredDomainTypes.Add(domainType);
                RegisterSingleGenericEntity(services, configType, domainType);
            }
            else if (genericDef == dualGenericConfigBase)
            {
                // GSheetEntityConfig<TRow, TDomain> — separate row + domain
                var rowType = genericArgs[0];
                var domainType = genericArgs[1];
                configuredDomainTypes.Add(domainType);
                RegisterDualGenericEntity(services, configType, rowType, domainType);
            }
        }

        // ── Tier 1: [GSheetEntity] types without a config class ──
        foreach (var domainType in types.Where(t =>
                     t is { IsAbstract: false, IsInterface: false } &&
                     t.GetCustomAttribute<GSheetEntityAttribute>() is not null &&
                     !configuredDomainTypes.Contains(t)))
            RegisterAttributeOnlyEntity(services, domainType);

        return services;
    }

    private static void RegisterAttributeOnlyEntity(IServiceCollection services, Type domainType)
    {
        var attr = domainType.GetCustomAttribute<GSheetEntityAttribute>()!;

        // Build adapter: EntityPipelineAdapter<TDomain, TDomain>
        var builderType = typeof(GSheetEntityBuilder<>).MakeGenericType(domainType);
        var builder = Activator.CreateInstance(builderType)!;

        var adapterType = typeof(EntityPipelineAdapter<,>).MakeGenericType(domainType, domainType);
        var buildMethod = builderType.BaseType!.GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var adapter = buildMethod.Invoke(builder, [attr.SheetName, attr.Range])!;

        var registration = new EntityRegistration(adapterType, attr.SheetName, attr.Range, []);
        GSheetImporterRegistry.RegisterEntityAdapter(registration, adapter);

        // Register the adapter instance as IGSheetPipeline<TDomain, TDomain>
        var pipelineInterface = typeof(IGSheetPipeline<,>).MakeGenericType(domainType, domainType);
        services.AddSingleton(pipelineInterface, adapter);
        services.AddSingleton(adapterType, adapter);
    }

    private static void RegisterSingleGenericEntity(IServiceCollection services, Type configType, Type domainType)
    {
        var attr = domainType.GetCustomAttribute<GSheetEntityAttribute>();
        var sheetName = attr?.SheetName ?? throw new InvalidOperationException(
            $"Entity config '{configType.Name}' targets domain type '{domainType.Name}' which is missing [GSheetEntity] attribute.");
        var range = attr.Range;

        // Create config instance and run Configure
        var configInstance = Activator.CreateInstance(configType)!;
        var builderType = typeof(GSheetEntityBuilder<>).MakeGenericType(domainType);
        var builder = Activator.CreateInstance(builderType)!;

        var configureMethod = configType.GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance)!;
        configureMethod.Invoke(configInstance, [builder]);

        var adapterType = typeof(EntityPipelineAdapter<,>).MakeGenericType(domainType, domainType);
        var buildMethod = builderType.BaseType!.GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var adapter = buildMethod.Invoke(builder, [sheetName, range])!;

        // Collect dependencies from builder
        var depsProperty =
            builderType.BaseType!.GetProperty("ExplicitDependencies", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var deps = (List<Type>)depsProperty.GetValue(builder)!;

        // Also collect child relationship dependencies
        var childRelsProperty =
            builderType.BaseType!.GetProperty("ChildRelationships", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var childRels = (System.Collections.IList)childRelsProperty.GetValue(builder)!;
        foreach (var rel in childRels)
        {
            var childType = (Type)rel.GetType().GetProperty("ChildType")!.GetValue(rel)!;
            if (!deps.Contains(childType))
                deps.Add(childType);
        }

        var registration = new EntityRegistration(adapterType, sheetName, range, [.. deps]);
        GSheetImporterRegistry.RegisterEntityAdapter(registration, adapter);

        var pipelineInterface = typeof(IGSheetPipeline<,>).MakeGenericType(domainType, domainType);
        services.AddSingleton(pipelineInterface, adapter);
        services.AddSingleton(adapterType, adapter);
    }

    private static void RegisterDualGenericEntity(IServiceCollection services, Type configType, Type rowType,
        Type domainType)
    {
        // Create config instance and run Configure
        var configInstance = Activator.CreateInstance(configType)!;
        var builderType = typeof(GSheetEntityBuilder<,>).MakeGenericType(rowType, domainType);
        var builder = Activator.CreateInstance(builderType)!;

        var configureMethod = configType.GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance)!;
        configureMethod.Invoke(configInstance, [builder]);

        // Get sheet/range from builder (set via Sheet() call) or from [GSheetEntity] on domain
        var sheetNameProp = builderType.GetProperty("SheetName", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var rangeProp = builderType.GetProperty("Range", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var builderSheet = (string?)sheetNameProp.GetValue(builder);
        var builderRange = (string?)rangeProp.GetValue(builder);

        // Fallback to [GSheetEntity] on row type
        var rowAttr = rowType.GetCustomAttribute<GSheetEntityAttribute>();
        var sheetName = builderSheet ?? rowAttr?.SheetName
            ?? throw new InvalidOperationException(
                $"Entity config '{configType.Name}' must call Sheet() or have [GSheetEntity] on row/domain type.");
        var range = builderRange ?? rowAttr?.Range ?? "";

        var adapterType = typeof(EntityPipelineAdapter<,>).MakeGenericType(rowType, domainType);
        var buildMethod = builderType.GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var adapter = buildMethod.Invoke(builder, [sheetName, range])!;

        // Collect dependencies
        var depsProperty =
            builderType.GetProperty("ExplicitDependencies", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var deps = (List<Type>)depsProperty.GetValue(builder)!;

        var childRelsProperty =
            builderType.GetProperty("ChildRelationships", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var childRels = (System.Collections.IList)childRelsProperty.GetValue(builder)!;
        foreach (var rel in childRels)
        {
            var childType = (Type)rel.GetType().GetProperty("ChildType")!.GetValue(rel)!;
            if (!deps.Contains(childType))
                deps.Add(childType);
        }

        var registration = new EntityRegistration(adapterType, sheetName, range, [.. deps]);
        GSheetImporterRegistry.RegisterEntityAdapter(registration, adapter);

        var pipelineInterface = typeof(IGSheetPipeline<,>).MakeGenericType(rowType, domainType);
        services.AddSingleton(pipelineInterface, adapter);
        services.AddSingleton(adapterType, adapter);
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<GSheetOptions>();
            var credential = ResolveCredential(opts.Credentials);
            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = opts.ApplicationName
            });
        });

        services.AddSingleton<GSheetImportContext>();
        services.AddSingleton<GSheetConnector>();
        services.AddSingleton<GSheetParserService>();
        services.AddSingleton<GSheetImportOrchestrator>();
        services.AddSingleton<GSheetConfigSource>();
        services.AddSingleton<IConfigSource>(sp => sp.GetRequiredService<GSheetConfigSource>());
        services.AddSingleton<GSheetImporterRegistry>();
    }

    private static GoogleCredential ResolveCredential(GSheetCredentials credentials)
    {
        switch (credentials.Method)
        {
            case GSheetCredentialsMethod.Base64JsonFromConfiguration:
                if (string.IsNullOrWhiteSpace(credentials.Value))
                    throw new InvalidOperationException("Credentials.Value must contain Base64 JSON content.");
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(credentials.Value));
                    return GoogleCredential.FromJson(json).CreateScoped(SheetsService.Scope.Spreadsheets);
                }
            case GSheetCredentialsMethod.CredentialsBase64Env:
                var envName = string.IsNullOrWhiteSpace(credentials.Value)
                    ? "GOOGLE_SHEET_CREDENTIALS"
                    : credentials.Value;
                var base64 = Environment.GetEnvironmentVariable(envName);
                if (string.IsNullOrWhiteSpace(base64))
                    throw new InvalidOperationException(
                        $"Environment variable '{envName}' does not contain credentials.");
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                    return GoogleCredential.FromJson(json).CreateScoped(SheetsService.Scope.Spreadsheets);
                }
            case GSheetCredentialsMethod.Path:
                if (string.IsNullOrWhiteSpace(credentials.Value))
                    throw new InvalidOperationException("Credentials.Value must contain path to credentials JSON.");
                using (var stream = new FileStream(credentials.Value, FileMode.Open, FileAccess.Read))
                {
                    return GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(credentials.Method));
        }
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
}
