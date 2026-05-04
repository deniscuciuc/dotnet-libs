using Microsoft.Extensions.Configuration;

namespace DenisCuciuc.Platform.MongoDB;

public static class MongoDBOptionsExtensions
{
    public static MongoDBOptions GetMongoDBOptions(
        this IConfiguration configuration,
        string sectionPath = MongoDBOptions.DefaultSectionPath)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(sectionPath).Get<MongoDBOptions>();
        return options ?? throw new InvalidOperationException(
            $"Configuration section '{sectionPath}' is missing or invalid for MongoDBOptions.");
    }
}
