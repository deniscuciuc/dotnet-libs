using System.ComponentModel.DataAnnotations;

namespace DenisCuciuc.Platform.Jobs.Hangfire.Tests;

public class PlatformJobsOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new PlatformJobsOptions();

        Assert.Equal(JobProviderKind.Hangfire, options.Provider);
        Assert.Equal(JobStoreKind.Postgres, options.Store);
        Assert.Equal(3, options.MaxRetries);
        Assert.Equal(60, options.DefaultTimeoutSeconds);
        Assert.False(options.Dashboard.Enabled);
        Assert.Equal("/jobs", options.Dashboard.Path);
        Assert.Equal(DashboardAuthMode.Identity, options.Dashboard.AuthMode);
    }

    [Fact]
    public void MaxRetries_OutOfRange_FailsValidation()
    {
        var options = new PlatformJobsOptions { MaxRetries = 11 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PlatformJobsOptions.MaxRetries)));
    }

    [Fact]
    public void MaxRetries_AtBoundary_Zero_PassesValidation()
    {
        var options = new PlatformJobsOptions { MaxRetries = 0 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.True(valid);
    }

    [Fact]
    public void DefaultTimeoutSeconds_OutOfRange_Below_FailsValidation()
    {
        var options = new PlatformJobsOptions { DefaultTimeoutSeconds = 0 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PlatformJobsOptions.DefaultTimeoutSeconds)));
    }

    [Fact]
    public void DefaultTimeoutSeconds_AtBoundary_Min_PassesValidation()
    {
        var options = new PlatformJobsOptions { DefaultTimeoutSeconds = 1 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.True(valid);
    }

    [Fact]
    public void DefaultTimeoutSeconds_AtBoundary_Max_PassesValidation()
    {
        var options = new PlatformJobsOptions { DefaultTimeoutSeconds = 3600 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.True(valid);
    }

    [Fact]
    public void DefaultSectionPath_Is_Jobs()
    {
        Assert.Equal("Jobs", PlatformJobsOptions.DefaultSectionPath);
    }
}
