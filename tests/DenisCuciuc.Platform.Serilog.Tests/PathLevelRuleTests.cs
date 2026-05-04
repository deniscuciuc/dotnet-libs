using DenisCuciuc.Platform.Serilog.RequestLogging;

namespace DenisCuciuc.Platform.Serilog.Tests;

public class PathLevelRuleTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var rule = new PathLevelRule();

        Assert.Null(rule.Path);
        Assert.Null(rule.Level);
        Assert.True(rule.PrefixMatch);
    }
}
