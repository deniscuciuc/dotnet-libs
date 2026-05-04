namespace DenisCuciuc.Platform.Jobs.Tests;

public class PlatformJobAttributeTests
{
    [Fact]
    public void Defaults_AllFallbackValues()
    {
        var attr = new PlatformJobAttribute();

        Assert.Equal(-1, attr.Retries);
        Assert.Equal(-1, attr.TimeoutSeconds);
        Assert.Null(attr.Queue);
    }

    [Fact]
    public void SetProperties_ReturnsConfiguredValues()
    {
        var attr = new PlatformJobAttribute
        {
            Retries = 5,
            TimeoutSeconds = 120,
            Queue = "critical"
        };

        Assert.Equal(5, attr.Retries);
        Assert.Equal(120, attr.TimeoutSeconds);
        Assert.Equal("critical", attr.Queue);
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        var attr = typeof(DecoratedJob)
            .GetCustomAttributes(typeof(PlatformJobAttribute), false)
            .OfType<PlatformJobAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attr);
        Assert.Equal(3, attr.Retries);
        Assert.Equal(30, attr.TimeoutSeconds);
        Assert.Equal("emails", attr.Queue);
    }

    [PlatformJob(Retries = 3, TimeoutSeconds = 30, Queue = "emails")]
    private sealed class DecoratedJob : IPlatformJob
    {
        public Task ExecuteAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
