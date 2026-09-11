using Microsoft.AspNetCore.Builder;

namespace CoreLibs.Sentry.Tests;

public class SentryExtensionsTests
{
    [Fact]
    public void AddCoreSentry_DoesNotThrow_WhenDisabled()
    {
        var builder = WebApplication.CreateBuilder();

        var result = builder.AddCoreSentry(o =>
        {
            o.Enabled = false;
            o.Dsn = null;
        });

        Assert.Same(builder, result);
    }

    [Fact]
    public void AddCoreSentry_DoesNotThrow_WhenDsnIsNull()
    {
        var builder = WebApplication.CreateBuilder();

        var result = builder.AddCoreSentry(o =>
        {
            o.Enabled = true;
            o.Dsn = null;
        });

        Assert.Same(builder, result);
    }

    [Fact]
    public void AddCoreSentry_DoesNotThrow_WhenDsnIsEmpty()
    {
        var builder = WebApplication.CreateBuilder();

        var result = builder.AddCoreSentry(o =>
        {
            o.Enabled = true;
            o.Dsn = string.Empty;
        });

        Assert.Same(builder, result);
    }

    [Fact]
    public void AddCoreSentry_AllowsCustomConfiguration()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddCoreSentry(o =>
        {
            o.Enabled = false;
            o.Environment = "testing";
            o.TracesSampleRate = 0.5;
            o.Debug = true;
            o.Release = "test@1.0.0";
            o.ServerName = "test-server";
        });
    }
}
