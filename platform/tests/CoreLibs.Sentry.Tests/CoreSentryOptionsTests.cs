namespace CoreLibs.Sentry.Tests;

public class CoreSentryOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new CoreSentryOptions();

        Assert.True(options.Enabled);
        Assert.Null(options.Dsn);
        Assert.Equal("development", options.Environment);
        Assert.Equal(0.1, options.TracesSampleRate);
        Assert.False(options.Debug);
        Assert.Null(options.Release);
        Assert.Null(options.ServerName);
    }

    [Fact]
    public void DefaultSectionPath_Is_Sentry()
    {
        Assert.Equal("Sentry", CoreSentryOptions.DefaultSectionPath);
    }

    [Fact]
    public void Enabled_CanBeSetToFalse()
    {
        var options = new CoreSentryOptions { Enabled = false };
        Assert.False(options.Enabled);
    }

    [Fact]
    public void Dsn_CanBeSet()
    {
        const string dsn = "https://key@sentry.io/123";
        var options = new CoreSentryOptions { Dsn = dsn };
        Assert.Equal(dsn, options.Dsn);
    }

    [Fact]
    public void TracesSampleRate_CanBeSetToZero()
    {
        var options = new CoreSentryOptions { TracesSampleRate = 0.0 };
        Assert.Equal(0.0, options.TracesSampleRate);
    }

    [Fact]
    public void TracesSampleRate_CanBeSetToOne()
    {
        var options = new CoreSentryOptions { TracesSampleRate = 1.0 };
        Assert.Equal(1.0, options.TracesSampleRate);
    }

    [Fact]
    public void Release_CanBeSet()
    {
        var options = new CoreSentryOptions { Release = "my-api@1.2.3" };
        Assert.Equal("my-api@1.2.3", options.Release);
    }

    [Fact]
    public void ServerName_CanBeOverridden()
    {
        var options = new CoreSentryOptions { ServerName = "my-server" };
        Assert.Equal("my-server", options.ServerName);
    }
}
