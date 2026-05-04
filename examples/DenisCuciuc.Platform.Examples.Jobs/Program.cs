using DenisCuciuc.Platform.Configuration;
using DenisCuciuc.Platform.Examples.Jobs;
using DenisCuciuc.Platform.HealthCheck;
using DenisCuciuc.Platform.Jobs;
using DenisCuciuc.Platform.Jobs.Hangfire;
using DenisCuciuc.Platform.Jobs.Startup;
using DenisCuciuc.Platform.Metrics;
using DenisCuciuc.Platform.Serilog;
using DenisCuciuc.Platform.Serilog.Bootstrap;
using DenisCuciuc.Platform.Serilog.Configuration;
using DenisCuciuc.Platform.Startup;
using DenisCuciuc.Platform.Swagger.Theme;

PlatformSerilogBootstrap.Configure();

var builder = WebApplication.CreateBuilder(args);

// â”€â”€ Configuration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.AddPlatformConfiguration(o => o.Profile = ConfigurationProfile.Default);

// â”€â”€ Observability â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Host.UsePlatformSerilog(o =>
{
    o.Profile = SerilogProfile.WebApi;
    o.ApplicationName = "DenisCuciuc.Platform.Examples.Jobs";
});

// â”€â”€ Jobs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddPlatformJobs(builder.Configuration);
builder.Services.AddPlatformJobsFromAssemblyOf<Program>();
builder.Services.AddPlatformRecurringJob<CleanupJob>("cleanup", "0 0 * * *");
builder.Services.AddPlatformJobsStartup();

// â”€â”€ Health, Metrics & API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddPlatformHealthChecks();
builder.Services.AddPlatformMetrics(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "DenisCuciuc.Platform Jobs API", Version = "v1" });
});

var app = builder.Build();

// â”€â”€ Middleware â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.UsePlatformSwagger();
app.UsePlatformHealthChecks();
app.MapPlatformMetrics();
app.MapPlatformJobsDashboard();

// â”€â”€ Endpoints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
var jobs = app.MapGroup("/jobs").WithTags("Jobs");

jobs.MapPost("/send-email", async (IJobScheduler scheduler) =>
{
    await scheduler.EnqueueAsync<SendEmailJob>();
    return Results.Accepted();
})
.WithName("EnqueueSendEmail")
.WithSummary("Enqueue a send-email job for immediate execution")
.Produces(StatusCodes.Status202Accepted);

jobs.MapPost("/send-email-delayed", async (IJobScheduler scheduler) =>
{
    await scheduler.ScheduleAsync<SendEmailJob>(TimeSpan.FromMinutes(5));
    return Results.Accepted();
})
.WithName("ScheduleSendEmail")
.WithSummary("Schedule a send-email job with a 5-minute delay")
.Produces(StatusCodes.Status202Accepted);

jobs.MapPost("/cleanup/trigger", async (IJobScheduler scheduler) =>
{
    await scheduler.TriggerRecurringAsync("cleanup");
    return Results.Accepted();
})
.WithName("TriggerCleanup")
.WithSummary("Trigger the daily cleanup recurring job immediately")
.Produces(StatusCodes.Status202Accepted);

// â”€â”€ Start â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
await app.RunWithStartupAsync();

