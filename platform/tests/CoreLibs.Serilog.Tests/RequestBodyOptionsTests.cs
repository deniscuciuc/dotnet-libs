using CoreLibs.Serilog.RequestLogging;

namespace CoreLibs.Serilog.Tests;

public class RequestBodyOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new RequestBodyOptions();

        Assert.False(options.LogRequestBody);
        Assert.False(options.LogResponseBody);
        Assert.Equal(2048, options.MaxRequestBodyLength);
        Assert.Equal(4096, options.MaxResponseBodyLength);
    }

    [Fact]
    public void LogRequestBody_CanBeEnabled()
    {
        var options = new RequestBodyOptions { LogRequestBody = true };
        Assert.True(options.LogRequestBody);
    }

    [Fact]
    public void LogResponseBody_CanBeEnabled()
    {
        var options = new RequestBodyOptions { LogResponseBody = true };
        Assert.True(options.LogResponseBody);
    }

    [Fact]
    public void MaxLengths_CanBeCustomized()
    {
        var options = new RequestBodyOptions
        {
            MaxRequestBodyLength = 512,
            MaxResponseBodyLength = 1024
        };

        Assert.Equal(512, options.MaxRequestBodyLength);
        Assert.Equal(1024, options.MaxResponseBodyLength);
    }
}
