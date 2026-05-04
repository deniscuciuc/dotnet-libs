using DenisCuciuc.Platform.CQRS;
using ErrorOr;
using MediatR;

namespace DenisCuciuc.Platform.Examples.WebApi.Notes;

public static class DeleteNote
{
    public sealed record Command(string Id) : ICommand;

    internal sealed class Handler(INoteRepository repository, IPublisher publisher) : ICommandHandler<Command>
    {
        public async Task<ErrorOr<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            var deleted = await repository.DeleteAsync(request.Id);

            if (!deleted)
                return Error.NotFound("Note.NotFound", $"Note '{request.Id}' was not found.");

            await publisher.Publish(new NoteDeletedEvent(request.Id), cancellationToken);

            return Unit.Value;
        }
    }
}
