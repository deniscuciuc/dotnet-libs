using DenisCuciuc.Platform.CQRS;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DenisCuciuc.Platform.Examples.WebApi.Notes;

public static class UpdateNote
{
    public sealed record Command(string Id, string Title, string Content) : ICommand;

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Content).NotEmpty().MaximumLength(10_000);
        }
    }

    internal sealed class Handler(INoteRepository repository, IPublisher publisher) : ICommandHandler<Command>
    {
        public async Task<ErrorOr<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            var updated = await repository.UpdateAsync(request.Id, request.Title, request.Content);

            if (!updated)
                return Error.NotFound("Note.NotFound", $"Note '{request.Id}' was not found.");

            await publisher.Publish(new NoteUpdatedEvent(request.Id, request.Title), cancellationToken);

            return Unit.Value;
        }
    }
}
