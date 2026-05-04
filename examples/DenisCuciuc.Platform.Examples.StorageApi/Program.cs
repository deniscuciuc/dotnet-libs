using DenisCuciuc.Platform.Configuration;
using DenisCuciuc.Platform.HealthCheck;
using DenisCuciuc.Platform.Metrics;
using DenisCuciuc.Platform.Sentry;
using DenisCuciuc.Platform.Serilog;
using DenisCuciuc.Platform.Startup;
using DenisCuciuc.Platform.Storage;
using DenisCuciuc.Platform.Storage.Abstractions;
using DenisCuciuc.Platform.Storage.Startup;
using DenisCuciuc.Platform.Swagger.Theme;

var builder = WebApplication.CreateBuilder(args);

// â”€â”€ Configuration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.AddPlatformConfiguration(o => o.Profile = ConfigurationProfile.Default);

// â”€â”€ Observability â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Host.UsePlatformSerilog();
builder.AddPlatformSentry();

// â”€â”€ Infrastructure â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddPlatformFileStorage(builder.Configuration);
builder.Services.AddPlatformStorageStartup();

// â”€â”€ Application â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddPlatformHealthChecks();
builder.Services.AddPlatformMetrics(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// â”€â”€ Middleware â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.UsePlatformSwagger();
app.UsePlatformHealthChecks();
app.MapPlatformMetrics();

const string defaultBucket = "uploads";

// â”€â”€ Endpoints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.MapGet("/", () => Results.Ok(new { message = "DenisCuciuc.Platform Storage + MinIO Example" }))
    .WithName("Root");

app.MapPost("/files", async (IFormFile file, IFileStorage storage) =>
{
    await using var stream = file.OpenReadStream();
    var ext = Path.GetExtension(file.FileName);
    var key = $"{Guid.NewGuid():N}{ext}";

    var metadata = await storage.UploadAsync(defaultBucket, key, stream, file.ContentType);

    return Results.Created($"/files/{key}", new
    {
        key,
        originalName = file.FileName,
        metadata.Size,
        metadata.ContentType,
        metadata.ETag
    });
})
.DisableAntiforgery()
.WithName("UploadFile");

app.MapGet("/files", async (IFileStorage storage, string? prefix) =>
{
    var objects = await storage.ListAsync(defaultBucket, prefix);
    return Results.Ok(objects);
})
.WithName("ListFiles");

app.MapGet("/files/{*key}", async (string key, IFileStorage storage) =>
{
    if (!await storage.ExistsAsync(defaultBucket, key))
        return Results.NotFound(new { error = $"File '{key}' not found." });

    var meta = await storage.GetMetadataAsync(defaultBucket, key);
    var stream = await storage.DownloadAsync(defaultBucket, key);
    return Results.File(stream, meta.ContentType, Path.GetFileName(key));
})
.WithName("DownloadFile");

app.MapDelete("/files/{*key}", async (string key, IFileStorage storage) =>
{
    await storage.DeleteAsync(defaultBucket, key);
    return Results.NoContent();
})
.WithName("DeleteFile");

app.MapGet("/files/url/{*key}", async (string key, IFileStorage storage, int? expiryMinutes) =>
{
    if (!await storage.ExistsAsync(defaultBucket, key))
        return Results.NotFound(new { error = $"File '{key}' not found." });

    var url = await storage.GetPresignedUrlAsync(defaultBucket, key,
        TimeSpan.FromMinutes(expiryMinutes ?? 60));
    return Results.Ok(new { url, expiresIn = $"{expiryMinutes ?? 60} minutes" });
})
.WithName("GetPresignedUrl");

// â”€â”€ Start â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
await app.RunWithStartupAsync();
