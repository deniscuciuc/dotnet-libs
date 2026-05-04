using DenisCuciuc.Platform.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DenisCuciuc.Platform.Jobs.Startup;

public static class JobsStartupExtensions
{
    /// <summary>
    /// Registers the DenisCuciuc.Platform jobs startup pipeline:
    /// <see cref="JobsConnectStartup"/> (order 0), <see cref="JobsRecurringStartup"/> (order 1),
    /// and a <see cref="StartupGuard{T}"/> for <see cref="IJobScheduler"/>.
    /// </summary>
    public static IServiceCollection AddPlatformJobsStartup(this IServiceCollection services)
    {
        services.AddPlatformStartup<JobsConnectStartup>();
        services.AddPlatformStartup<JobsRecurringStartup>();
        services.AddPlatformStartupGuard<IJobScheduler>();
        return services;
    }

    /// <summary>
    /// Registers a recurring job that will be created at application startup.
    /// </summary>
    public static IServiceCollection AddPlatformRecurringJob<TJob>(
        this IServiceCollection services,
        string jobId,
        string cron)
        where TJob : class, IPlatformJob
    {
        services.TryAddScoped<TJob>();

        JobsRecurringStartup.Registrations.Add(
            new JobsRecurringStartup.RecurringJobRegistration(typeof(TJob), jobId, cron));

        return services;
    }

    /// <summary>
    /// Scans the assembly containing <typeparamref name="TMarker"/> for <see cref="IPlatformJob"/>
    /// implementations and registers them in DI as scoped services.
    /// </summary>
    public static IServiceCollection AddPlatformJobsFromAssemblyOf<TMarker>(this IServiceCollection services)
    {
        var jobTypes = typeof(TMarker).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IPlatformJob).IsAssignableFrom(t));

        foreach (var type in jobTypes)
        {
            services.TryAddScoped(type);
        }

        return services;
    }

    /// <summary>
    /// Marks the <see cref="StartupGuard{T}"/> for <see cref="IJobScheduler"/> as configured.
    /// </summary>
    public static void MarkJobsConfigured(this IHostStartup hostStartup) =>
        hostStartup.MarkConfigured<IJobScheduler>();
}
