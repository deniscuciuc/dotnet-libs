using System.ComponentModel.DataAnnotations;
using DenisCuciuc.Platform.Postgres;

namespace DenisCuciuc.Platform.Postgres.Tests;

public class PostgresOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new PostgresOptions();

        Assert.Equal(string.Empty, options.ConnectionString);
        Assert.Equal(3, options.MaxRetryCount);
        Assert.Equal(30, options.CommandTimeout);
        Assert.False(options.EnableSensitiveDataLogging);
        Assert.False(options.EnableDetailedErrors);
        Assert.False(options.EnableDynamicJson);
        Assert.Equal(PostgresNamingConvention.SnakeCase, options.NamingConvention);
    }

    [Fact]
    public void DefaultSectionPath_Is_Postgres()
    {
        Assert.Equal("Postgres", PostgresOptions.DefaultSectionPath);
    }

    [Fact]
    public void ConnectionString_IsRequired()
    {
        var options = new PostgresOptions { ConnectionString = "" };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PostgresOptions.ConnectionString)));
    }

    [Fact]
    public void MaxRetryCount_OutOfRange_Below_FailsValidation()
    {
        var options = new PostgresOptions { ConnectionString = "x", MaxRetryCount = -1 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PostgresOptions.MaxRetryCount)));
    }

    [Fact]
    public void MaxRetryCount_OutOfRange_Above_FailsValidation()
    {
        var options = new PostgresOptions { ConnectionString = "x", MaxRetryCount = 11 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PostgresOptions.MaxRetryCount)));
    }

    [Fact]
    public void MaxRetryCount_AtBoundary_Zero_PassesValidation()
    {
        var options = new PostgresOptions { ConnectionString = "x", MaxRetryCount = 0 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.True(valid);
    }

    [Fact]
    public void MaxRetryCount_AtBoundary_Ten_PassesValidation()
    {
        var options = new PostgresOptions { ConnectionString = "x", MaxRetryCount = 10 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.True(valid);
    }

    [Fact]
    public void CommandTimeout_OutOfRange_Below_FailsValidation()
    {
        var options = new PostgresOptions { ConnectionString = "x", CommandTimeout = 0 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PostgresOptions.CommandTimeout)));
    }

    [Fact]
    public void CommandTimeout_OutOfRange_Above_FailsValidation()
    {
        var options = new PostgresOptions { ConnectionString = "x", CommandTimeout = 301 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PostgresOptions.CommandTimeout)));
    }

    [Fact]
    public void CommandTimeout_AtBoundary_One_PassesValidation()
    {
        var options = new PostgresOptions { ConnectionString = "x", CommandTimeout = 1 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.True(valid);
    }

    [Fact]
    public void CommandTimeout_AtBoundary_ThreeHundred_PassesValidation()
    {
        var options = new PostgresOptions { ConnectionString = "x", CommandTimeout = 300 };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.True(valid);
    }
}
