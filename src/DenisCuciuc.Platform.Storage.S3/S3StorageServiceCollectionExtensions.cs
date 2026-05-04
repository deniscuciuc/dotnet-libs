using Amazon.S3;
using DenisCuciuc.Platform.Storage.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DenisCuciuc.Platform.Storage.S3;

public static class S3StorageServiceCollectionExtensions
{
    public static IServiceCollection AddS3Storage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<S3StorageOptions>()
            .Bind(configuration.GetSection(S3StorageOptions.DefaultSectionPath))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton<IAmazonS3>(sp =>
        {
            var s3Options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<S3StorageOptions>>().CurrentValue;

            var config = new AmazonS3Config
            {
                ServiceURL = s3Options.Endpoint,
                ForcePathStyle = s3Options.ForcePathStyle,
                AuthenticationRegion = s3Options.Region,
            };

            return new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, config);
        });

        services.TryAddSingleton<IFileStorage, S3FileStorage>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStorageInitializer, S3BucketInitializer>());

        return services;
    }
}
