using DenisCuciuc.Platform.Domain;

namespace DenisCuciuc.Platform.Examples.WebApi.Notes;

public sealed record NoteCreatedEvent(string NoteId, string Title) : DomainEvent;

public sealed record NoteUpdatedEvent(string NoteId, string Title) : DomainEvent;

public sealed record NoteDeletedEvent(string NoteId) : DomainEvent;
