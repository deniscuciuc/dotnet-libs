using CoreLibs.CQRS;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace CoreLibs.Examples.WebApi.Notes;

public static class CreateNote
{
    public sealed record Command(string Title, string Content) : ICommand<string>;

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Content).NotEmpty().MaximumLength(10_000);
        }
    }

    internal sealed class Handler(INoteRepository repository, IPublisher publisher) : ICommandHandler<Command, string>
    {
        public async Task<ErrorOr<string>> Handle(Command request, CancellationToken cancellationToken)
        {
            var entity = new NoteEntity
            {
                Title = request.Title,
                Content = request.Content
            };

            await repository.InsertAsync(entity);

            await publisher.Publish(new NoteCreatedEvent(entity.StringId, entity.Title), cancellationToken);

            return entity.StringId;
        }
    }
}
