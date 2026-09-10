using CoreLibs.CQRS;
using ErrorOr;

namespace CoreLibs.Examples.WebApi.Notes;

public static class GetNote
{
    public sealed record Query(string Id) : ICacheableQuery<NoteResponse>
    {
        public QueryCachePolicy CachePolicy => QueryCachePolicy.Absolute(
            $"note:{Id}",
            TimeSpan.FromMinutes(5));
    }

    internal sealed class Handler(INoteRepository repository) : IQueryHandler<Query, NoteResponse>
    {
        public async Task<ErrorOr<NoteResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entity = await repository.GetByIdAsync(request.Id);

            if (entity is null)
                return Error.NotFound("Note.NotFound", $"Note '{request.Id}' was not found.");

            return new NoteResponse(
                entity.StringId,
                entity.Title,
                entity.Content,
                entity.CreatedAt,
                entity.ModifiedAt);
        }
    }
}
