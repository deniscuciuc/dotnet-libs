using DenisCuciuc.Platform.Serilog.Configuration;

namespace DenisCuciuc.Platform.Serilog.Tests;

public class PlatformSerilogOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new PlatformSerilogOptions();

        Assert.Null(options.ApplicationName);
        Assert.Equal(SerilogProfile.Default, options.Profile);
        Assert.True(options.EnableMachineName);
        Assert.True(options.EnableEnvironment);
        Assert.True(options.EnableCorrelationId);
    }

    [Fact]
    public void AllProfiles_AreDefinedInEnum()
    {
        var profiles = Enum.GetValues<SerilogProfile>();

        Assert.Contains(SerilogProfile.Default, profiles);
        Assert.Contains(SerilogProfile.WebApi, profiles);
        Assert.Contains(SerilogProfile.Worker, profiles);
        Assert.Contains(SerilogProfile.Minimal, profiles);
        Assert.Contains(SerilogProfile.Debug, profiles);
    }

    [Fact]
    public void Options_CanDisableAllEnrichers()
    {
        var options = new PlatformSerilogOptions
        {
            EnableMachineName = false,
            EnableEnvironment = false,
            EnableCorrelationId = false
        };

        Assert.False(options.EnableMachineName);
        Assert.False(options.EnableEnvironment);
        Assert.False(options.EnableCorrelationId);
    }
}
