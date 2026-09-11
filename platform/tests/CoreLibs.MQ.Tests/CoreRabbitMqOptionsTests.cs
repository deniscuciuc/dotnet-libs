using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace CoreLibs.MQ.Tests;

public class CoreRabbitMqOptionsTests
{
    [Fact]
    public void DefaultSectionPath_Is_Correct()
    {
        Assert.Equal("MQ:Rabbit", CoreRabbitMqOptions.DefaultSectionPath);
    }

    [Fact]
    public void Host_IsRequired()
    {
        var options = new CoreRabbitMqOptions { Host = "", VirtualHost = "/", Username = "u", Password = "p" };
        var ctx = new DataAnnotations.ValidationContext(options);
        var results = new List<DataAnnotations.ValidationResult>();

        var valid = DataAnnotations.Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CoreRabbitMqOptions.Host)));
    }

    [Fact]
    public void Username_IsRequired()
    {
        var options = new CoreRabbitMqOptions { Host = "h", VirtualHost = "/", Username = "", Password = "p" };
        var ctx = new DataAnnotations.ValidationContext(options);
        var results = new List<DataAnnotations.ValidationResult>();

        var valid = DataAnnotations.Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CoreRabbitMqOptions.Username)));
    }

    [Fact]
    public void Password_IsRequired()
    {
        var options = new CoreRabbitMqOptions { Host = "h", VirtualHost = "/", Username = "u", Password = "" };
        var ctx = new DataAnnotations.ValidationContext(options);
        var results = new List<DataAnnotations.ValidationResult>();

        var valid = DataAnnotations.Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CoreRabbitMqOptions.Password)));
    }

    [Fact]
    public void ValidOptions_PassValidation()
    {
        var options = new CoreRabbitMqOptions
        {
            Host = "rabbitmq://localhost",
            VirtualHost = "/",
            Username = "guest",
            Password = "guest"
        };
        var ctx = new DataAnnotations.ValidationContext(options);
        var results = new List<DataAnnotations.ValidationResult>();

        var valid = DataAnnotations.Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        Assert.True(valid);
    }

    [Fact]
    public void EndpointDefaults_HaveExpectedValues()
    {
        var defaults = new RabbitMqEndpointDefaults();

        Assert.Equal(1, defaults.PrefetchCount);
        Assert.Equal(0, defaults.ConcurrentMessageLimit);
        Assert.True(defaults.Durable);
        Assert.False(defaults.AutoDelete);
        Assert.Equal(3, defaults.RetryCount);
        Assert.Equal(3, defaults.RetryIntervalInSeconds);
        Assert.Equal(0, defaults.ConsumerTimeoutInSeconds);
        Assert.Equal(RabbitMqQueueType.Classic, defaults.QueueType);
        Assert.Equal(0, defaults.DeliveryLimit);
    }

    [Fact]
    public void EndpointDefaults_PrefetchCount_OutOfRange_FailsValidation()
    {
        var defaults = new RabbitMqEndpointDefaults { PrefetchCount = 0 }; // Range is [1, ushort.MaxValue]
        var ctx = new DataAnnotations.ValidationContext(defaults);
        var results = new List<DataAnnotations.ValidationResult>();

        var valid = DataAnnotations.Validator.TryValidateObject(defaults, ctx, results, validateAllProperties: true);

        Assert.False(valid);
    }
}
