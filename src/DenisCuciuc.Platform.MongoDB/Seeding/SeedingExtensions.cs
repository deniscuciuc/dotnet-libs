using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.MongoDB.Seeding;

public static class SeedingExtensions
{
    public static IServiceCollection AddSeeders<TSeeder>(this IServiceCollection services)
        where TSeeder : Seeder
    {
        services.AddSingleton<TSeeder>();
        services.AddSingleton<Seeder, TSeeder>();
        services.AddSingleton<ISeederRunner, SeederRunner>();
        return services;
    }

    public static IServiceCollection AddSeedersInAssemblyOfType<TSeeder>(this IServiceCollection services)
        where TSeeder : Seeder
    {
        var seederBaseType = typeof(Seeder);
        var types = new[] { typeof(TSeeder).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                type is { IsClass: true, IsAbstract: false, IsInterface: false }
                && seederBaseType.IsAssignableFrom(type)
            );

        foreach (var impl in types)
        {
            services.AddSingleton(typeof(Seeder), impl);
            services.AddSingleton(impl);
        }

        services.AddSingleton<ISeederRunner, SeederRunner>();

        return services;
    }
}
