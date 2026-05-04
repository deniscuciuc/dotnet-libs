using DenisCuciuc.Platform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DenisCuciuc.Platform.Postgres;

/// <summary>
/// EF Core interceptor that dispatches domain events from tracked <see cref="Entity"/> instances
/// after a successful <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> call.
/// </summary>
public sealed class DomainEventSaveChangesInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null)
            DispatchDomainEventsAsync(eventData.Context, default).GetAwaiter().GetResult();

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        var entities = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entities.Count > 0)
            await dispatcher.DispatchEventsAsync(entities, cancellationToken);
    }
}
