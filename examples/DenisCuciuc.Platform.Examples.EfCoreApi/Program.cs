using DenisCuciuc.Platform.Configuration;
using DenisCuciuc.Platform.CQRS;
using DenisCuciuc.Platform.Domain;
using DenisCuciuc.Platform.ErrorHandling;
using DenisCuciuc.Platform.Examples.EfCoreApi.Data;
using DenisCuciuc.Platform.Examples.EfCoreApi.Features;
using DenisCuciuc.Platform.HealthCheck;
using DenisCuciuc.Platform.Metrics;
using DenisCuciuc.Platform.Postgres;
using DenisCuciuc.Platform.Postgres.Startup;
using DenisCuciuc.Platform.Sentry;
using DenisCuciuc.Platform.Serilog;
using DenisCuciuc.Platform.Startup;
using DenisCuciuc.Platform.Swagger.Theme;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// â”€â”€ Configuration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.AddPlatformConfiguration(o => o.Profile = ConfigurationProfile.Default);

// â”€â”€ Observability â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Host.UsePlatformSerilog();
builder.AddPlatformSentry();

// â”€â”€ Infrastructure â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddPlatformDomainEvents();
builder.Services.AddPlatformPostgres<AppDbContext>(builder.Configuration);
builder.Services.AddPlatformPostgresStartup<AppDbContext>();

// â”€â”€ Application â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddPlatformCqrsFromAssemblyOf<Program>(builder.Configuration);
builder.Services.AddPlatformHealthChecks().AddPostgresHealthCheck();
builder.Services.AddPlatformMetrics(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// â”€â”€ Middleware â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.UsePlatformSwagger();
app.UsePlatformHealthChecks();
app.MapPlatformMetrics();

// â”€â”€ Endpoints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.MapGet("/", () => Results.Ok(new { message = "DenisCuciuc.Platform EF Core + PostgreSQL Example" }))
    .WithName("Root");

app.MapPost("/products", async (CreateProductRequest request, ISender sender) =>
{
    var result = await sender.Send(new CreateProduct.Command(
        request.Name, request.Description, request.Price, request.Stock));
    return result.Match(
        id => Results.Created($"/products/{id}", new { id }),
        errors => errors.ToProblemResult());
})
.WithName("CreateProduct");

app.MapGet("/products/{id:guid}", async (Guid id, ISender sender) =>
{
    var result = await sender.Send(new GetProduct.Query(id));
    return result.Match(
        product => Results.Ok(product),
        errors => errors.ToProblemResult());
})
.WithName("GetProduct");

app.MapGet("/products", async (int? page, int? pageSize, ISender sender) =>
{
    var result = await sender.Send(new ListProducts.Query(page ?? 1, pageSize ?? 20));
    return result.Match(
        paged => Results.Ok(paged),
        errors => errors.ToProblemResult());
})
.WithName("ListProducts");

// â”€â”€ Start â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
await app.RunWithStartupAsync();

// â”€â”€ Request DTOs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
public record CreateProductRequest(string Name, string Description, decimal Price, int Stock);
