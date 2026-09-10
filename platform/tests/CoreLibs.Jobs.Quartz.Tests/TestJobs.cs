namespace CoreLibs.Jobs.Quartz.Tests;

public class QuartzSimpleJob : ICoreJob
{
    public Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
}

[CoreJob(Queue = "custom-queue")]
public class QueuedJob : ICoreJob
{
    public Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
}
