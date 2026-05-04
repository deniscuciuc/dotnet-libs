using DenisCuciuc.Platform.Serilog.CorrelationId;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DenisCuciuc.Platform.Serilog.Tests;

public class CorrelationIdExtensionsTests
{
    [Fact]
    public void AddPlatformCorrelationId_RegistersHttpContextAccessor()
    {
        var services = new ServiceCollection();
        services.AddPlatformCorrelationId();

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetService<IHttpContextAccessor>();

        Assert.NotNull(accessor);
    }
}
