using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreLibs.Startup;

public static class StartupExtensions
{
    // -------------------------------------------------------------------------
    // IHostStartup helpers  (backward-compat + new guard names)
    // -------------------------------------------------------------------------

    /// <summary>Marks <see cref="StartupGuard{T}"/> as configured.</summary>
    public static void MarkConfigured<T>(this IHostStartup startup) =>
        startup.Services.MarkConfigured<T>();

    /// <summary>Marks <see cref="StartupGuard{T}"/> as configured via <see cref="IServiceProvider"/>.</summary>
    public static void MarkConfigured<T>(this IServiceProvider provider)
    {
        var guard = provider.GetService<StartupGuard<T>>();
        guard?.Configure();
    }

    // -------------------------------------------------------------------------
    // DI registration
    // -------------------------------------------------------------------------

    /// <summary>Registers a <see cref="StartupGuard{T}"/> that fails the host if <typeparamref name="T"/> is not configured.</summary>
    public static IServiceCollection AddCoreStartupGuard<T>(this IServiceCollection services)
    {
        services.AddSingleton<StartupGuard<T>>();
        services.AddSingleton<IStartup>(x => x.GetRequiredService<StartupGuard<T>>());
        return services;
    }

    /// <summary>Registers a concrete <see cref="IStartup"/> task.</summary>
    public static IServiceCollection AddCoreStartup<T>(this IServiceCollection services)
        where T : class, IStartup
    {
        services.AddSingleton<T>();
        services.AddSingleton<IStartup>(x => x.GetRequiredService<T>());
        return services;
    }

    /// <summary>
    /// Scans the assembly containing <typeparamref name="TMarker"/> for classes decorated with
    /// <see cref="StartupTaskAttribute"/> and registers each as an <see cref="IStartup"/> singleton.
    /// </summary>
    public static IServiceCollection AddCoreStartupTasksFromAssemblyOf<TMarker>(this IServiceCollection services)
    {
        var types = typeof(TMarker).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.GetCustomAttribute<StartupTaskAttribute>() is not null
                        && typeof(IStartup).IsAssignableFrom(t));

        foreach (var type in types)
        {
            services.AddSingleton(type);
            services.AddSingleton(typeof(IStartup), sp => sp.GetRequiredService(type));
        }

        return services;
    }

    // -------------------------------------------------------------------------
    // Orchestration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Runs the pre-startup hook, executes all <see cref="IStartup"/> tasks ordered by
    /// <see cref="IStartup.Order"/>, then starts the host. Environment-restricted tasks
    /// (via <see cref="StartupTaskAttribute.Environments"/>) are skipped when they don't
    /// match the current hosting environment.
    /// </summary>
    public static async Task RunWithStartupAsync(
        this IHost application,
        Func<IHostStartup, Task>? preStartup = null,
        CancellationToken cancellationToken = default)
    {
        var host = new HostStartupImpl(application);

        // Pre-startup hook (infrastructure wiring: MongoDB, Redis, …)
        if (preStartup is not null)
            await preStartup(host);

        // Ordered startup tasks
        var tasks = application.Services
            .GetServices<IStartup>()
            .OrderBy(t => t.Order)
            .ToList();

        foreach (var task in tasks)
        {
            if (!ShouldRun(task, host.Environment))
            {
                host.Logger.LogDebug("Skipping startup task {Task} (not in environment {Env})",
                    task.GetType().Name, host.Environment.EnvironmentName);
                continue;
            }

            host.Logger.LogInformation("Running startup task {Task} (order {Order}) ...",
                task.GetType().Name, task.Order);

            await task.StartupAsync(host, cancellationToken);

            host.Logger.LogInformation("Completed startup task {Task}",
                task.GetType().Name);
        }

        await application.RunAsync(cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static bool ShouldRun(IStartup task, IHostEnvironment env)
    {
        var attr = task.GetType().GetCustomAttribute<StartupTaskAttribute>();
        if (attr is null || attr.Environments.Length == 0)
            return true;

        return attr.Environments.Any(e =>
            string.Equals(e, env.EnvironmentName, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class HostStartupImpl : IHostStartup
    {
        public HostStartupImpl(IHost application)
        {
            Services = application.Services;
            Configuration = Services.GetRequiredService<IConfiguration>();
            Environment = Services.GetRequiredService<IHostEnvironment>();
            Logger = Services.GetRequiredService<ILoggerFactory>().CreateLogger("CoreLibs.Startup");
        }

        public IServiceProvider Services { get; }
        public IConfiguration Configuration { get; }
        public IHostEnvironment Environment { get; }
        public ILogger Logger { get; }
    }
}
