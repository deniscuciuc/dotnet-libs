using DenisCuciuc.Platform.CQRS;
using DenisCuciuc.Platform.Examples.EfCoreApi.Data;
using DenisCuciuc.Platform.Examples.EfCoreApi.Products;
using ErrorOr;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DenisCuciuc.Platform.Examples.EfCoreApi.Features;

public static class ListProducts
{
    public sealed record Query(int Page, int PageSize) : IQuery<PagedResult<ProductResponse>>;

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    internal sealed class Handler(AppDbContext db) : IQueryHandler<Query, PagedResult<ProductResponse>>
    {
        public async Task<ErrorOr<PagedResult<ProductResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var totalCount = await db.Products.CountAsync(cancellationToken);

            var items = await db.Products
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProductResponse(
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Stock,
                    p.CreatedAt,
                    p.ModifiedAt))
                .ToListAsync(cancellationToken);

            return PagedResult<ProductResponse>.Create(items, totalCount, request.Page, request.PageSize);
        }
    }
}
