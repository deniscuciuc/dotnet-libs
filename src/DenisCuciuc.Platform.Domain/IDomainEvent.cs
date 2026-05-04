using MediatR;

namespace DenisCuciuc.Platform.Domain;

public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
