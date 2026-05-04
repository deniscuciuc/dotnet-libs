using Microsoft.AspNetCore.Builder;

namespace DenisCuciuc.Platform.Sentry.Tests;

public class SentryExtensionsTests
{
    [Fact]
    public void AddPlatformSentry_DoesNotThrow_WhenDisabled()
    {
        var builder = WebApplication.CreateBuilder();

        var result = builder.AddPlatformSentry(o =>
        {
            o.Enabled = false;
            o.Dsn = null;
        });

        Assert.Same(builder, result);
    }

    [Fact]
    public void AddPlatformSentry_DoesNotThrow_WhenDsnIsNull()
    {
        var builder = WebApplication.CreateBuilder();

        var result = builder.AddPlatformSentry(o =>
        {
            o.Enabled = true;
            o.Dsn = null;
        });

        Assert.Same(builder, result);
    }

    [Fact]
    public void AddPlatformSentry_DoesNotThrow_WhenDsnIsEmpty()
    {
        var builder = WebApplication.CreateBuilder();

        var result = builder.AddPlatformSentry(o =>
        {
            o.Enabled = true;
            o.Dsn = string.Empty;
        });

        Assert.Same(builder, result);
    }

    [Fact]
    public void AddPlatformSentry_AllowsCustomConfiguration()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddPlatformSentry(o =>
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
