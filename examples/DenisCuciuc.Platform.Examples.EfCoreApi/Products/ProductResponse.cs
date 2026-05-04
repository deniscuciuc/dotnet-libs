namespace DenisCuciuc.Platform.Examples.EfCoreApi.Products;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    DateTime ModifiedAt);
