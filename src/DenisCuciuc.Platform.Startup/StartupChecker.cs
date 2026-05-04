namespace DenisCuciuc.Platform.Startup;

/// <summary>
/// Ensures component <typeparamref name="T"/> is explicitly configured during the pre-startup
/// phase. If <see cref="Configure"/> is never called the guard throws on host start, giving a
/// clear error instead of a cryptic runtime failure.
/// </summary>
public sealed class StartupGuard<T>(string? message = null) : IStartup
{
    public string Message { get; } =
        message ?? $"{typeof(T).Name} is not configured. Ensure its startup task ran or call MarkConfigured<{typeof(T).Name}>().";

    private bool Configured { get; set; }

    public int Order => int.MaxValue; // guards always run last

    public Task StartupAsync(IHostStartup host, CancellationToken cancellationToken = default)
    {
        if (!Configured)
            throw new InvalidOperationException(Message);

        return Task.CompletedTask;
    }

    public void Configure() => Configured = true;
}
