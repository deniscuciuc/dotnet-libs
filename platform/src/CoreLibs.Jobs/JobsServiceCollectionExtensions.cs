using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Jobs;

public static class JobsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the CoreLibs.Platform job scheduling system using configuration.
    /// Provider and store are selected dynamically based on the <c>Jobs</c> section.
    /// </summary>
    public static IServiceCollection AddCoreJobs(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = CoreJobsOptions.DefaultSectionPath)
    {
        services
            .AddOptions<CoreJobsOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration
                          .GetSection(sectionPath)
                          .Get<CoreJobsOptions>()
                      ?? new CoreJobsOptions();

        RegisterProvider(services, configuration, options);

        return services;
    }

    /// <summary>
    /// Registers the CoreLibs.Platform job scheduling system using a delegate.
    /// </summary>
    public static IServiceCollection AddCoreJobs(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CoreJobsOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        services
            .AddOptions<CoreJobsOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = new CoreJobsOptions();
        configureOptions(options);

        RegisterProvider(services, configuration, options);

        return services;
    }

    /// <summary>
    /// Registers an <see cref="InMemoryJobScheduler"/> for testing purposes.
    /// </summary>
    public static IServiceCollection AddCoreJobsInMemory(this IServiceCollection services)
    {
        var scheduler = new InMemoryJobScheduler();
        services.AddSingleton(scheduler);
        services.AddSingleton<IJobScheduler>(scheduler);
        return services;
    }

    private static void RegisterProvider(
        IServiceCollection services,
        IConfiguration configuration,
        CoreJobsOptions options)
    {
        // Step 1: Register the provider adapter (Hangfire or Quartz)
        var (providerType, providerAssembly, providerMethod) = options.Provider switch
        {
            JobProviderKind.Hangfire => (
                "CoreLibs.Jobs.Hangfire.HangfireJobsServiceCollectionExtensions",
                "CoreLibs.Jobs.Hangfire",
                "AddHangfireJobs"),
            JobProviderKind.Quartz => (
                "CoreLibs.Jobs.Quartz.QuartzJobsServiceCollectionExtensions",
                "CoreLibs.Jobs.Quartz",
                "AddQuartzJobs"),
            _ => throw new InvalidOperationException($"Unsupported job provider: {options.Provider}")
        };

        InvokeRegistration(providerType, providerAssembly, providerMethod, services, configuration);

        // Step 2: Register the job store (InMemory uses the provider's built-in default)
        if (options.Store == JobStoreKind.InMemory)
            return;

        var storePrefix = options.Provider switch
        {
            JobProviderKind.Hangfire => "CoreLibs.Jobs.Hangfire",
            JobProviderKind.Quartz => "CoreLibs.Jobs.Quartz",
            _ => throw new InvalidOperationException($"Unsupported job provider: {options.Provider}")
        };

        var (storeType, storeAssembly, storeMethod) = options.Store switch
        {
            JobStoreKind.MongoDB => (
                $"{storePrefix}.MongoDB.{GetStoreClassName(options.Provider, JobStoreKind.MongoDB)}",
                $"{storePrefix}.MongoDB",
                GetStoreMethodName(options.Provider, JobStoreKind.MongoDB)),
            JobStoreKind.Postgres => (
                $"{storePrefix}.Postgres.{GetStoreClassName(options.Provider, JobStoreKind.Postgres)}",
                $"{storePrefix}.Postgres",
                GetStoreMethodName(options.Provider, JobStoreKind.Postgres)),
            _ => throw new InvalidOperationException($"Unsupported job store: {options.Store}")
        };

        InvokeRegistration(storeType, storeAssembly, storeMethod, services, configuration);
    }

    private static string GetStoreClassName(JobProviderKind provider, JobStoreKind store)
    {
        return (provider, store) switch
        {
            (JobProviderKind.Hangfire, JobStoreKind.MongoDB) => "HangfireMongoDBStoreExtensions",
            (JobProviderKind.Hangfire, JobStoreKind.Postgres) => "HangfirePostgresStoreExtensions",
            (JobProviderKind.Quartz, JobStoreKind.MongoDB) => "QuartzMongoDBStoreExtensions",
            (JobProviderKind.Quartz, JobStoreKind.Postgres) => "QuartzPostgresStoreExtensions",
            _ => throw new InvalidOperationException($"Unsupported provider/store combination: {provider}/{store}")
        };
    }

    private static string GetStoreMethodName(JobProviderKind provider, JobStoreKind store)
    {
        return (provider, store) switch
        {
            (JobProviderKind.Hangfire, JobStoreKind.MongoDB) => "AddHangfireMongoDBStore",
            (JobProviderKind.Hangfire, JobStoreKind.Postgres) => "AddHangfirePostgresStore",
            (JobProviderKind.Quartz, JobStoreKind.MongoDB) => "AddQuartzMongoDBStore",
            (JobProviderKind.Quartz, JobStoreKind.Postgres) => "AddQuartzPostgresStore",
            _ => throw new InvalidOperationException($"Unsupported provider/store combination: {provider}/{store}")
        };
    }

    private static void InvokeRegistration(
        string typeName,
        string assemblyName,
        string methodName,
        IServiceCollection services,
        IConfiguration configuration)
    {
        var type = Type.GetType($"{typeName}, {assemblyName}")
                   ?? throw new InvalidOperationException(
                       $"Job provider package '{assemblyName}' is not referenced. Add the package and retry.");

        var method = type.GetMethod(
                         methodName,
                         [typeof(IServiceCollection), typeof(IConfiguration)])
                     ?? throw new InvalidOperationException(
                         $"Provider registration method '{methodName}' was not found in '{typeName}'.");

        method.Invoke(null, [services, configuration]);
    }
}
