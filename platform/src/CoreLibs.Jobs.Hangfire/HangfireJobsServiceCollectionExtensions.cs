using CoreLibs.Jobs.Hangfire.Filters;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoreLibs.Jobs.Hangfire;

public static class HangfireJobsServiceCollectionExtensions
{
    /// <summary>
    /// Registers Hangfire as the CoreLibs.Platform job scheduling provider.
    /// Called via reflection from <see cref="JobsServiceCollectionExtensions"/>.
    /// </summary>
    public static IServiceCollection AddHangfireJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire((sp, config) =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<CoreJobsOptions>>().CurrentValue;

            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseInMemoryStorage(); // default; overridden by store extensions (Postgres, MongoDB)

            config.UseFilter(new CoreJobRetryFilter(options));
            config.UseFilter(new CoreJobLoggingFilter());
            config.UseFilter(new CoreJobCorrelationFilter());
            config.UseFilter(new CoreJobMetricsFilter());
        });

        services.AddHangfireServer(options => { options.WorkerCount = Environment.ProcessorCount * 2; });

        services.AddScoped<HangfireJobScheduler>();
        services.AddScoped<IJobScheduler>(sp => sp.GetRequiredService<HangfireJobScheduler>());

        return services;
    }
}
