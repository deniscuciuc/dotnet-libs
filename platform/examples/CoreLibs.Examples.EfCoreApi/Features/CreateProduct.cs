using CoreLibs.CQRS;
using CoreLibs.Examples.EfCoreApi.Data;
using CoreLibs.Examples.EfCoreApi.Products;
using ErrorOr;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreLibs.Examples.EfCoreApi.Features;

public static class CreateProduct
{
    public sealed record Command(string Name, string Description, decimal Price, int Stock)
        : ICommand<Guid>;

    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Price).GreaterThan(0);
            RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        }
    }

    internal sealed class Handler(AppDbContext db) : ICommandHandler<Command, Guid>
    {
        public async Task<ErrorOr<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var exists = await db.Products.AnyAsync(p => p.Name == request.Name, cancellationToken);
            if (exists)
                return Error.Conflict("Product.Duplicate", $"A product named '{request.Name}' already exists.");

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock
            };

            db.Products.Add(product);
            await db.SaveChangesAsync(cancellationToken);

            return product.Id;
        }
    }
}
