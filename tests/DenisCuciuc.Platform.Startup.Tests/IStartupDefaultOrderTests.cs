namespace DenisCuciuc.Platform.Startup.Tests;

public class IStartupDefaultOrderTests
{
    [Fact]
    public void DefaultOrder_IsZero()
    {
        IStartup task = new SampleStartupTask();
        Assert.Equal(0, task.Order);
    }
}
