using CoreLibs.Configuration;
using CoreLibs.CQRS;
using CoreLibs.Domain;
using CoreLibs.ErrorHandling;
using CoreLibs.Examples.WebApi.Notes;
using CoreLibs.HealthCheck;
using CoreLibs.Metrics;
using CoreLibs.MongoDB;
using CoreLibs.MongoDB.Startup;
using CoreLibs.Redis;
using CoreLibs.Redis.Startup;
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
builder.Services.AddCoreRedis(builder.Configuration);
builder.Services.AddCoreRedisCache();
builder.Services.AddCoreRedisStartup();

builder.Services.AddMongoDB(builder.Configuration);
builder.Services.AddSingleton<NoteIndexes>();
builder.Services.AddRepository<INoteRepository, NoteRepository>();
builder.Services.AddCoreMongoDBStartup();

// â”€â”€ Application â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddCoreCqrsFromAssemblyOf<Program>(builder.Configuration);
builder.Services.AddCoreHealthChecks();
builder.Services.AddCoreMetrics(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// â”€â”€ Middleware â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.UseCoreSwagger();
app.UseCoreHealthChecks();
app.MapCoreMetrics();

// â”€â”€ Endpoints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.MapGet("/", () => Results.Ok(new { message = "CoreLibs WebApi Example" }))
    .WithName("Root");

app.MapPost("/notes", async (CreateNoteRequest request, ISender sender) =>
{
    var result = await sender.Send(new CreateNote.Command(request.Title, request.Content));
    return result.Match(
        id => Results.Created($"/notes/{id}", new { id }),
        errors => errors.ToProblemResult());
})
.WithName("CreateNote");

app.MapGet("/notes/{id}", async (string id, ISender sender) =>
{
    var result = await sender.Send(new GetNote.Query(id));
    return result.Match(
        note => Results.Ok(note),
        errors => errors.ToProblemResult());
})
.WithName("GetNote");

app.MapGet("/notes", async (int? page, int? pageSize, ISender sender) =>
{
    var result = await sender.Send(new ListNotes.Query(page ?? 1, pageSize ?? 20));
    return result.Match(
        paged => Results.Ok(paged),
        errors => errors.ToProblemResult());
})
.WithName("ListNotes");

app.MapPut("/notes/{id}", async (string id, UpdateNoteRequest request, ISender sender) =>
{
    var result = await sender.Send(new UpdateNote.Command(id, request.Title, request.Content));
    return result.Match(
        _ => Results.NoContent(),
        errors => errors.ToProblemResult());
})
.WithName("UpdateNote");

app.MapDelete("/notes/{id}", async (string id, ISender sender) =>
{
    var result = await sender.Send(new DeleteNote.Command(id));
    return result.Match(
        _ => Results.NoContent(),
        errors => errors.ToProblemResult());
})
.WithName("DeleteNote");

// â”€â”€ Start â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
await app.RunWithStartupAsync();

// â”€â”€ Request DTOs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
public record CreateNoteRequest(string Title, string Content);
public record UpdateNoteRequest(string Title, string Content);
