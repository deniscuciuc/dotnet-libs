namespace CoreLibs.Jobs.Tests;

public class CoreJobAttributeTests
{
    [Fact]
    public void Defaults_AllFallbackValues()
    {
        var attr = new CoreJobAttribute();

        Assert.Equal(-1, attr.Retries);
        Assert.Equal(-1, attr.TimeoutSeconds);
        Assert.Null(attr.Queue);
    }

    [Fact]
    public void SetProperties_ReturnsConfiguredValues()
    {
        var attr = new CoreJobAttribute
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
            .GetCustomAttributes(typeof(CoreJobAttribute), false)
            .OfType<CoreJobAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attr);
        Assert.Equal(3, attr.Retries);
        Assert.Equal(30, attr.TimeoutSeconds);
        Assert.Equal("emails", attr.Queue);
    }

    [CoreJob(Retries = 3, TimeoutSeconds = 30, Queue = "emails")]
    private sealed class DecoratedJob : ICoreJob
    {
        public Task ExecuteAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
