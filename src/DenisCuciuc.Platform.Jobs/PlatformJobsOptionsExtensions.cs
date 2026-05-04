using Microsoft.Extensions.Configuration;

namespace DenisCuciuc.Platform.Jobs;

public static class PlatformJobsOptionsExtensions
{
    /// <summary>
    /// Reads <see cref="PlatformJobsOptions"/> from configuration.
    /// </summary>
    public static PlatformJobsOptions GetQGJobsOptions(
        this IConfiguration configuration,
        string sectionPath = PlatformJobsOptions.DefaultSectionPath)
    {
        return configuration.GetSection(sectionPath).Get<PlatformJobsOptions>() ?? new PlatformJobsOptions();
    }
}
