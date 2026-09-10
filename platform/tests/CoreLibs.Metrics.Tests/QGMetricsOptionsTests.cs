using System.ComponentModel.DataAnnotations;

namespace CoreLibs.Metrics.Tests;

public class CoreMetricsOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new CoreMetricsOptions();

        Assert.True(options.Enabled);
        Assert.True(options.EnableAspNetCoreInstrumentation);
        Assert.Equal("/metrics", options.EndpointPath);
        Assert.Empty(options.Meters);
    }

    [Fact]
    public void DefaultSectionPath_Is_Metrics()
    {
        Assert.Equal("Metrics", CoreMetricsOptions.DefaultSectionPath);
    }

    [Fact]
    public void EndpointPath_Validation_MustStartWithSlash()
    {
        var options = new CoreMetricsOptions { EndpointPath = "metrics" }; // missing leading slash

        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CoreMetricsOptions.EndpointPath)));
    }

    [Fact]
    public void EndpointPath_Validation_PassesWithLeadingSlash()
    {
        var options = new CoreMetricsOptions { EndpointPath = "/custom-metrics" };

        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        Assert.True(isValid);
    }

    [Fact]
    public void Meters_CanBeConfigured()
    {
        var options = new CoreMetricsOptions { Meters = ["MyApp.Metrics", "MyApp.Jobs"] };
        Assert.Equal(2, options.Meters.Length);
        Assert.Contains("MyApp.Metrics", options.Meters);
    }

    [Fact]
    public void Enabled_CanBeSetToFalse()
    {
        var options = new CoreMetricsOptions { Enabled = false };
        Assert.False(options.Enabled);
    }
}
