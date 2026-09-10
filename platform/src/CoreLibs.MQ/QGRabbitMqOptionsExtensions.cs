using Microsoft.Extensions.Configuration;

namespace CoreLibs.MQ;

public static class RabbitMqOptionsExtensions
{
    public static CoreRabbitMqOptions GetQGRabbitMqOptions(
        this IConfiguration configuration,
        string sectionPath = CoreRabbitMqOptions.DefaultSectionPath)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(sectionPath).Get<CoreRabbitMqOptions>();
        return options ?? throw new InvalidOperationException(
            $"Configuration section '{sectionPath}' is missing or invalid for CoreRabbitMqOptions.");
    }
}
