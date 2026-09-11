using Microsoft.Extensions.Configuration;

namespace CoreLibs.Jobs;

public static class CoreJobsOptionsExtensions
{
    /// <summary>
    /// Reads <see cref="CoreJobsOptions"/> from configuration.
    /// </summary>
    public static CoreJobsOptions GetQGJobsOptions(
        this IConfiguration configuration,
        string sectionPath = CoreJobsOptions.DefaultSectionPath)
    {
        return configuration.GetSection(sectionPath).Get<CoreJobsOptions>() ?? new CoreJobsOptions();
    }
}
