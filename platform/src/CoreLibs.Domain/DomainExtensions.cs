using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Domain;

public static class DomainExtensions
{
    /// <summary>
    /// Registers the MediatR-based <see cref="IDomainEventDispatcher"/>.
    /// </summary>
    public static IServiceCollection AddCoreDomainEvents(this IServiceCollection services)
    {
        services.AddSingleton<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        return services;
    }
}
