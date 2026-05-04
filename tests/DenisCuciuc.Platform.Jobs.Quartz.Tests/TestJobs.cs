namespace DenisCuciuc.Platform.Jobs.Quartz.Tests;

public class QuartzSimpleJob : IPlatformJob
{
    public Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
}

[PlatformJob(Queue = "custom-queue")]
public class QueuedJob : IPlatformJob
{
    public Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
}
