using CoreLibs.Configuration;
using CoreLibs.HealthCheck;
using CoreLibs.Metrics;
using CoreLibs.Sentry;
using CoreLibs.Serilog;
using CoreLibs.Startup;
using CoreLibs.Storage;
using CoreLibs.Storage.Abstractions;
using CoreLibs.Storage.Startup;
using CoreLibs.Swagger.Theme;

var builder = WebApplication.CreateBuilder(args);

// â”€â”€ Configuration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.AddCoreConfiguration(o => o.Profile = ConfigurationProfile.Default);

// â”€â”€ Observability â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Host.UseCoreSerilog();
builder.AddCoreSentry();

// â”€â”€ Infrastructure â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddCoreFileStorage(builder.Configuration);
builder.Services.AddCoreStorageStartup();

// â”€â”€ Application â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddCoreHealthChecks();
builder.Services.AddCoreMetrics(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// â”€â”€ Middleware â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.UseCoreSwagger();
app.UseCoreHealthChecks();
app.MapCoreMetrics();

const string defaultBucket = "uploads";

// â”€â”€ Endpoints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.MapGet("/", () => Results.Ok(new { message = "CoreLibs Storage + MinIO Example" }))
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
