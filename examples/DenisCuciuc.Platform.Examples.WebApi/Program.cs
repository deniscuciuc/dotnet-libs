using DenisCuciuc.Platform.Configuration;
using DenisCuciuc.Platform.CQRS;
using DenisCuciuc.Platform.Domain;
using DenisCuciuc.Platform.ErrorHandling;
using DenisCuciuc.Platform.Examples.WebApi.Notes;
using DenisCuciuc.Platform.HealthCheck;
using DenisCuciuc.Platform.Metrics;
using DenisCuciuc.Platform.MongoDB;
using DenisCuciuc.Platform.MongoDB.Startup;
using DenisCuciuc.Platform.Redis;
using DenisCuciuc.Platform.Redis.Startup;
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
builder.Services.AddPlatformRedis(builder.Configuration);
builder.Services.AddPlatformRedisCache();
builder.Services.AddPlatformRedisStartup();

builder.Services.AddMongoDB(builder.Configuration);
builder.Services.AddSingleton<NoteIndexes>();
builder.Services.AddRepository<INoteRepository, NoteRepository>();
builder.Services.AddPlatformMongoDBStartup();

// â”€â”€ Application â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddPlatformCqrsFromAssemblyOf<Program>(builder.Configuration);
builder.Services.AddPlatformHealthChecks();
builder.Services.AddPlatformMetrics(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// â”€â”€ Middleware â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.UsePlatformSwagger();
app.UsePlatformHealthChecks();
app.MapPlatformMetrics();

// â”€â”€ Endpoints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.MapGet("/", () => Results.Ok(new { message = "DenisCuciuc.Platform WebApi Example" }))
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
