using DenisCuciuc.Platform.Serilog.RequestLogging;

namespace DenisCuciuc.Platform.Serilog.Tests;

public class RequestLoggingConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new RequestLoggingConfig();

        Assert.Equal("Information", config.DefaultLevel);
        Assert.Empty(config.PathLevels);
        Assert.NotNull(config.Body);
    }

    [Fact]
    public void PathLevels_CanBeConfigured()
    {
        var config = new RequestLoggingConfig
        {
            PathLevels =
            [
                new PathLevelRule { Path = "/health", Level = "Debug" },
                new PathLevelRule { Path = "/metrics", Level = "Verbose" }
            ]
        };

        Assert.Equal(2, config.PathLevels.Count);
    }
}
