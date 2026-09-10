namespace CoreLibs.Jobs.Hangfire.Tests;

public class SimpleTestJob : ICoreJob
{
    public bool Executed { get; private set; }

    public Task ExecuteAsync(CancellationToken ct)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}

public class ArgsTestJob : ICoreJob<string>
{
    public string? Args { get; set; }
    public bool Executed { get; private set; }

    public Task ExecuteAsync(CancellationToken ct)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}

public class ObjectArgsTestJob : ICoreJob<object>
{
    public object? Args { get; set; }
    public bool Executed { get; private set; }

    public Task ExecuteAsync(CancellationToken ct)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}
