using Microsoft.Extensions.Configuration;

namespace DenisCuciuc.Platform.MQ;

public static class RabbitMqOptionsExtensions
{
    public static PlatformRabbitMqOptions GetQGRabbitMqOptions(
        this IConfiguration configuration,
        string sectionPath = PlatformRabbitMqOptions.DefaultSectionPath)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(sectionPath).Get<PlatformRabbitMqOptions>();
        return options ?? throw new InvalidOperationException(
            $"Configuration section '{sectionPath}' is missing or invalid for PlatformRabbitMqOptions.");
    }
}
