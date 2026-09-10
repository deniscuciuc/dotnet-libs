namespace CoreLibs.Jobs.Tests;

public class CoreJobsOptionsTests
{
    [Fact]
    public void Defaults_UseHangfirePostgres()
    {
        var options = new CoreJobsOptions();

        Assert.Equal(JobProviderKind.Hangfire, options.Provider);
        Assert.Equal(JobStoreKind.Postgres, options.Store);
        Assert.Equal(3, options.MaxRetries);
        Assert.Equal(60, options.DefaultTimeoutSeconds);
    }

    [Fact]
    public void DashboardDefaults_Disabled()
    {
        var options = new CoreJobsOptions();

        Assert.False(options.Dashboard.Enabled);
        Assert.Equal("/jobs", options.Dashboard.Path);
        Assert.Equal(DashboardAuthMode.Identity, options.Dashboard.AuthMode);
    }

    [Fact]
    public void DefaultSectionPath_IsCorrect()
    {
        Assert.Equal("Jobs", CoreJobsOptions.DefaultSectionPath);
    }
}
