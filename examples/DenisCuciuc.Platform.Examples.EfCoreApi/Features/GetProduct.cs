using DenisCuciuc.Platform.CQRS;
using DenisCuciuc.Platform.Examples.EfCoreApi.Data;
using DenisCuciuc.Platform.Examples.EfCoreApi.Products;
using ErrorOr;
using Microsoft.EntityFrameworkCore;

namespace DenisCuciuc.Platform.Examples.EfCoreApi.Features;

public static class GetProduct
{
    public sealed record Query(Guid Id) : IQuery<ProductResponse>;

    internal sealed class Handler(AppDbContext db) : IQueryHandler<Query, ProductResponse>
    {
        public async Task<ErrorOr<ProductResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product is null)
                return Error.NotFound("Product.NotFound", $"Product '{request.Id}' was not found.");

            return new ProductResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Stock,
                product.CreatedAt,
                product.ModifiedAt);
        }
    }
}
