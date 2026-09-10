using CoreLibs.CQRS;
using ErrorOr;
using FluentValidation;

namespace CoreLibs.Examples.WebApi.Notes;

public static class ListNotes
{
    public sealed record Query(int Page, int PageSize) : IQuery<PagedResult<NoteResponse>>;

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    internal sealed class Handler(INoteRepository repository) : IQueryHandler<Query, PagedResult<NoteResponse>>
    {
        public async Task<ErrorOr<PagedResult<NoteResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await repository.GetPagedAsync(request.Page, request.PageSize);

            var mapped = items
                .Select(e => new NoteResponse(e.StringId, e.Title, e.Content, e.CreatedAt, e.ModifiedAt))
                .ToList();

            return PagedResult<NoteResponse>.Create(mapped, (int)totalCount, request.Page, request.PageSize);
        }
    }
}
