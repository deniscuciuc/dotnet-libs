namespace DenisCuciuc.Platform.Domain;

/// <summary>
/// Base class for all domain entities. Provides identity (Guid) and domain event support.
/// Uses UUIDv7 for time-ordered sequential inserts (better B-tree index performance).
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public bool HasDomainEvents => _domainEvents.Count != 0;

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
        if (_domainEvents.Count == 0)
            return Array.Empty<IDomainEvent>();

        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);
    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
