using System.ComponentModel.DataAnnotations;

namespace DenisCuciuc.Platform.Metrics.Tests;

public class PlatformMetricsOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new PlatformMetricsOptions();

        Assert.True(options.Enabled);
        Assert.True(options.EnableAspNetCoreInstrumentation);
        Assert.Equal("/metrics", options.EndpointPath);
        Assert.Empty(options.Meters);
    }

    [Fact]
    public void DefaultSectionPath_Is_Metrics()
    {
        Assert.Equal("Metrics", PlatformMetricsOptions.DefaultSectionPath);
    }

    [Fact]
    public void EndpointPath_Validation_MustStartWithSlash()
    {
        var options = new PlatformMetricsOptions { EndpointPath = "metrics" }; // missing leading slash

        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PlatformMetricsOptions.EndpointPath)));
    }

    [Fact]
    public void EndpointPath_Validation_PassesWithLeadingSlash()
    {
        var options = new PlatformMetricsOptions { EndpointPath = "/custom-metrics" };

        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.True(isValid);
    }

    [Fact]
    public void Meters_CanBeConfigured()
    {
        var options = new PlatformMetricsOptions { Meters = ["MyApp.Metrics", "MyApp.Jobs"] };
        Assert.Equal(2, options.Meters.Length);
        Assert.Contains("MyApp.Metrics", options.Meters);
    }

    [Fact]
    public void Enabled_CanBeSetToFalse()
    {
        var options = new PlatformMetricsOptions { Enabled = false };
        Assert.False(options.Enabled);
    }
}
