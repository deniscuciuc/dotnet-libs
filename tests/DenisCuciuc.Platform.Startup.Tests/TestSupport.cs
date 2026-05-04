using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DenisCuciuc.Platform.Startup.Tests;

internal class FakeHostStartup(IServiceProvider services) : IHostStartup
{
    public IServiceProvider Services => services;
    public IConfiguration Configuration =>
        new ConfigurationBuilder().Build();
    public IHostEnvironment Environment =>
        new FakeHostEnvironment();
    public ILogger Logger =>
        NullLogger.Instance;
}

internal class FakeHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "Test";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } =
        new NullFileProvider();
}

public class SampleStartupTask : IStartup
{
    public bool Executed { get; private set; }

    public Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}
