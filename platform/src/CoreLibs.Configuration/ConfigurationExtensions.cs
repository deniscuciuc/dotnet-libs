using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CoreLibs.Configuration;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Registers the CoreLibs.Platform opinionated YAML configuration pipeline.
    /// Layers are added in order: base → environment-specific → local override → environment variables.
    /// </summary>
    public static IHostApplicationBuilder AddCoreConfiguration(
        this IHostApplicationBuilder builder,
        Action<CoreConfigurationOptions>? configure = null)
    {
        var options = new CoreConfigurationOptions();
        configure?.Invoke(options);
        ApplyProfile(options);

        var env = builder.Environment.EnvironmentName;
        var baseName = options.BaseFileName;
        var ext = options.Extension;
        var config = builder.Configuration;

        config.AddYamlFile($"{baseName}.{ext}", options.Optional, options.ReloadOnChange);

        if (options.IncludeEnvironment)
            config.AddYamlFile($"{baseName}.{env}.{ext}", true, options.ReloadOnChange);

        if (options.IncludeLocal)
            config.AddYamlFile($"{baseName}.Local.{ext}", true, options.ReloadOnChange);

        if (options.IncludeEnvironmentVariables)
        {
            if (!string.IsNullOrEmpty(options.EnvironmentVariablesPrefix))
                config.AddEnvironmentVariables(options.EnvironmentVariablesPrefix);
            else
                config.AddEnvironmentVariables();
        }

        return builder;
    }

    /// <summary>
    /// Binds a configuration section to <typeparamref name="T"/> using a convention-based section name,
    /// enables data-annotation validation, and validates eagerly on host start so misconfigured apps fail fast.
    /// <para>
    /// Convention: strip the <c>Options</c> suffix and any leading <c>Core</c> prefix from the type name,
    /// then use the remaining type name as the section path.
    /// Examples: <c>MongoOptions</c> → <c>Mongo</c>, <c>CoreSentryOptions</c> → <c>Sentry</c>.
    /// </para>
    /// Supports <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/> for live reload.
    /// </summary>
    /// <param name="sectionName">
    /// Explicit section path. When <c>null</c> the convention-based name is used.
    /// </param>
    public static OptionsBuilder<T> AddCoreOptions<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
        where T : class
    {
        var section = sectionName ?? ResolveSection<T>();
        return services
            .AddOptions<T>()
            .Bind(configuration.GetSection(section))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    /// Prints all configuration key-value pairs to stdout.
    /// Values for sensitive keys (password, secret, token, key, connection string) are masked.
    /// An optional <paramref name="filter"/> predicate can restrict which keys are printed.
    /// </summary>
    public static void PrintConfiguration(
        this IConfiguration configuration,
        Func<string, bool>? filter = null)
    {
        Console.WriteLine("---- Configuration ----");

        foreach (var kvp in configuration.AsEnumerable())
        {
            if (filter != null && !filter(kvp.Key))
                continue;

            Console.WriteLine(IsSensitive(kvp.Key)
                ? $"{kvp.Key}=***"
                : $"{kvp.Key}={kvp.Value}");
        }

        Console.WriteLine("-----------------------");
    }

    /// <summary>
    /// Prints the ordered list of active configuration providers to stdout.
    /// Useful for diagnosing which sources are actually loaded and their precedence.
    /// </summary>
    public static void PrintSources(this IConfiguration configuration)
    {
        if (configuration is not IConfigurationRoot root)
            return;

        Console.WriteLine("---- Configuration Sources ----");

        foreach (var provider in root.Providers)
            Console.WriteLine(provider);

        Console.WriteLine("-------------------------------");
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies profile-level overrides after the caller's configure action so that
    /// profiles behave as opinionated presets. Users who need fine-grained control
    /// should leave <see cref="CoreConfigurationOptions.Profile"/> at Default and set
    /// the individual Include* flags directly.
    /// </summary>
    private static void ApplyProfile(CoreConfigurationOptions options)
    {
        switch (options.Profile)
        {
            case ConfigurationProfile.Minimal:
                options.IncludeEnvironment = false;
                options.IncludeLocal = false;
                break;

            case ConfigurationProfile.Cloud:
                options.IncludeLocal = false;
                options.IncludeEnvironmentVariables = true;
                break;

            case ConfigurationProfile.LocalDev:
                options.IncludeEnvironment = true;
                options.IncludeLocal = true;
                break;
        }
    }

    private static bool IsSensitive(string key) =>
        key.Contains("Password", StringComparison.OrdinalIgnoreCase)
        || key.Contains("Secret", StringComparison.OrdinalIgnoreCase)
        || key.Contains("Token", StringComparison.OrdinalIgnoreCase)
        || key.Contains("Key", StringComparison.OrdinalIgnoreCase)
        || key.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase);

    /// <remarks>
    /// Convention: strip trailing <c>Options</c> suffix, strip leading <c>Core</c> prefix, then use the remaining type name as-is.
    /// <c>MongoOptions</c> → <c>Mongo</c> | <c>CoreSentryOptions</c> → <c>Sentry</c>
    /// </remarks>
    private static string ResolveSection<T>()
    {
        var name = typeof(T).Name;

        if (name.EndsWith("Options", StringComparison.Ordinal))
            name = name[..^"Options".Length];

        if (name.StartsWith("Core", StringComparison.Ordinal) && name.Length > "Core".Length)
            name = name["Core".Length..];

        return name;
    }
}
