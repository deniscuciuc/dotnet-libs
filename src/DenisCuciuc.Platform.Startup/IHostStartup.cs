using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.Startup;

public interface IHostStartup
{
    IServiceProvider Services { get; }
    IConfiguration Configuration { get; }
    IHostEnvironment Environment { get; }
    ILogger Logger { get; }
}
