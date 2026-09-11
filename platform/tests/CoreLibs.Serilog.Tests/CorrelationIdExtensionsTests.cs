using CoreLibs.Serilog.CorrelationId;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CoreLibs.Serilog.Tests;

public class CorrelationIdExtensionsTests
{
    [Fact]
    public void AddCoreCorrelationId_RegistersHttpContextAccessor()
    {
        var services = new ServiceCollection();
        services.AddCoreCorrelationId();

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetService<IHttpContextAccessor>();

        Assert.NotNull(accessor);
    }
}
