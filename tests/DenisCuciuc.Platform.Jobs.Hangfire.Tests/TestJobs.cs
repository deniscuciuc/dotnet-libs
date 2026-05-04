namespace DenisCuciuc.Platform.Jobs.Hangfire.Tests;

public class SimpleTestJob : IPlatformJob
{
    public bool Executed { get; private set; }

    public Task ExecuteAsync(CancellationToken ct)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}

public class ArgsTestJob : IPlatformJob<string>
{
    public string? Args { get; set; }
    public bool Executed { get; private set; }

    public Task ExecuteAsync(CancellationToken ct)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}

public class ObjectArgsTestJob : IPlatformJob<object>
{
    public object? Args { get; set; }
    public bool Executed { get; private set; }

    public Task ExecuteAsync(CancellationToken ct)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}
