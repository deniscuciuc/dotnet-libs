using DenisCuciuc.Platform.Serilog.CorrelationId;

namespace DenisCuciuc.Platform.Serilog.Tests;

public class CorrelationIdOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new CorrelationIdOptions();

        Assert.Equal("X-Correlation-ID", options.HeaderName);
        Assert.True(options.IncludeInResponse);
    }
}
