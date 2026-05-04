using MediatR;

namespace DenisCuciuc.Platform.Examples.WebApi.Notes;

internal sealed class NoteCreatedEventHandler(ILogger<NoteCreatedEventHandler> logger)
    : INotificationHandler<NoteCreatedEvent>
{
    public Task Handle(NoteCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Note created: {NoteId} - {Title}", notification.NoteId, notification.Title);
        return Task.CompletedTask;
    }
}

internal sealed class NoteUpdatedEventHandler(ILogger<NoteUpdatedEventHandler> logger)
    : INotificationHandler<NoteUpdatedEvent>
{
    public Task Handle(NoteUpdatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Note updated: {NoteId} - {Title}", notification.NoteId, notification.Title);
        return Task.CompletedTask;
    }
}

internal sealed class NoteDeletedEventHandler(ILogger<NoteDeletedEventHandler> logger)
    : INotificationHandler<NoteDeletedEvent>
{
    public Task Handle(NoteDeletedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Note deleted: {NoteId}", notification.NoteId);
        return Task.CompletedTask;
    }
}
