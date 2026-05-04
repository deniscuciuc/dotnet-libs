namespace DenisCuciuc.Platform.Configuration.Tests;

public class PlatformConfigurationOptionsTests
{
    [Fact]
    public void DefaultValues()
    {
        var opts = new PlatformConfigurationOptions();

        Assert.Equal("appsettings", opts.BaseFileName);
        Assert.Equal("yml", opts.Extension);
        Assert.Equal(ConfigurationProfile.Default, opts.Profile);
        Assert.True(opts.IncludeEnvironment);
        Assert.False(opts.IncludeLocal);
        Assert.True(opts.IncludeEnvironmentVariables);
        Assert.Null(opts.EnvironmentVariablesPrefix);
        Assert.True(opts.Optional);
        Assert.True(opts.ReloadOnChange);
    }
}
