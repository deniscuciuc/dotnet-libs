using CoreLibs.Configuration;
using CoreLibs.Examples.Jobs;
using CoreLibs.HealthCheck;
using CoreLibs.Jobs;
using CoreLibs.Jobs.Hangfire;
using CoreLibs.Jobs.Startup;
using CoreLibs.Metrics;
using CoreLibs.Serilog;
using CoreLibs.Serilog.Bootstrap;
using CoreLibs.Serilog.Configuration;
using CoreLibs.Startup;
using CoreLibs.Swagger.Theme;

CoreSerilogBootstrap.Configure();

var builder = WebApplication.CreateBuilder(args);

// â”€â”€ Configuration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.AddCoreConfiguration(o => o.Profile = ConfigurationProfile.Default);

// â”€â”€ Observability â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Host.UseCoreSerilog(o =>
{
    o.Profile = SerilogProfile.WebApi;
    o.ApplicationName = "CoreLibs.Examples.Jobs";
});

// â”€â”€ Jobs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddCoreJobs(builder.Configuration);
builder.Services.AddCoreJobsFromAssemblyOf<Program>();
builder.Services.AddCoreRecurringJob<CleanupJob>("cleanup", "0 0 * * *");
builder.Services.AddCoreJobsStartup();

// â”€â”€ Health, Metrics & API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddCoreHealthChecks();
builder.Services.AddCoreMetrics(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CoreLibs Jobs API", Version = "v1" });
});

var app = builder.Build();

// â”€â”€ Middleware â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.UseCoreSwagger();
app.UseCoreHealthChecks();
app.MapCoreMetrics();
app.MapCoreJobsDashboard();

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

