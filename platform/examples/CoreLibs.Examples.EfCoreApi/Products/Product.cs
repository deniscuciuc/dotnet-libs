using CoreLibs.Domain;

namespace CoreLibs.Examples.EfCoreApi.Products;

public sealed class Product : Entity
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required decimal Price { get; set; }

    public required int Stock { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}
