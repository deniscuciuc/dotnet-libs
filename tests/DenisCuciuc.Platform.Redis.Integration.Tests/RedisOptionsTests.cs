using System.ComponentModel.DataAnnotations;
using DenisCuciuc.Platform.Redis;

namespace DenisCuciuc.Platform.Redis.Integration.Tests;

public class RedisOptionsTests
{
    [Fact]
    public void DefaultSectionPath_Is_Correct()
    {
        Assert.Equal("Redis", RedisOptions.DefaultSectionPath);
    }

    [Fact]
    public void DefaultConnectionString_IsNotEmpty()
    {
        var options = new RedisOptions();
        Assert.NotEmpty(options.ConnectionString);
    }

    [Fact]
    public void ConnectionString_IsRequired_WhenEmpty()
    {
        var options = new RedisOptions { ConnectionString = "" };
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RedisOptions.ConnectionString)));
    }
}
