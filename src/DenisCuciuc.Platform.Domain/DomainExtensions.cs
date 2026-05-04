using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.Domain;

public static class DomainExtensions
{
    /// <summary>
    /// Registers the MediatR-based <see cref="IDomainEventDispatcher"/>.
    /// </summary>
    public static IServiceCollection AddPlatformDomainEvents(this IServiceCollection services)
    {
        services.AddSingleton<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        return services;
    }
}
