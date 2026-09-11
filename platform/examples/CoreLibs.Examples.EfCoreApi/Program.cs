using CoreLibs.Configuration;
using CoreLibs.CQRS;
using CoreLibs.Domain;
using CoreLibs.ErrorHandling;
using CoreLibs.Examples.EfCoreApi.Data;
using CoreLibs.Examples.EfCoreApi.Features;
using CoreLibs.HealthCheck;
using CoreLibs.Metrics;
using CoreLibs.Postgres;
using CoreLibs.Postgres.Startup;
using CoreLibs.Sentry;
using CoreLibs.Serilog;
using CoreLibs.Startup;
using CoreLibs.Swagger.Theme;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// â”€â”€ Configuration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.AddCoreConfiguration(o => o.Profile = ConfigurationProfile.Default);

// â”€â”€ Observability â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Host.UseCoreSerilog();
builder.AddCoreSentry();

// â”€â”€ Infrastructure â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddCoreDomainEvents();
builder.Services.AddCorePostgres<AppDbContext>(builder.Configuration);
builder.Services.AddCorePostgresStartup<AppDbContext>();

// â”€â”€ Application â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddCoreCqrsFromAssemblyOf<Program>(builder.Configuration);
builder.Services.AddCoreHealthChecks().AddPostgresHealthCheck();
builder.Services.AddCoreMetrics(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// â”€â”€ Middleware â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.UseCoreSwagger();
app.UseCoreHealthChecks();
app.MapCoreMetrics();

// â”€â”€ Endpoints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.MapGet("/", () => Results.Ok(new { message = "CoreLibs EF Core + PostgreSQL Example" }))
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
