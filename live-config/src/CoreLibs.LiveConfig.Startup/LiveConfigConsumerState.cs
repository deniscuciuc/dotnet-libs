namespace CoreLibs.LiveConfig.Startup;

/// <summary>
/// Synchronization helper — consumers can await <see cref="WaitForInitialLoadAsync"/>
/// to block until the startup preload has completed.
/// </summary>
public sealed class LiveConfigConsumerState
{
    private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsReady => _tcs.Task.IsCompleted;

    /// <summary>
    /// Waits until initial config loading is complete.
    /// </summary>
    public Task WaitForInitialLoadAsync(CancellationToken cancellationToken = default)
    {
        return _tcs.Task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Signals that initial loading is complete.
    /// </summary>
    public void SetReady()
    {
        _tcs.TrySetResult();
    }

    /// <summary>
    /// Signals that initial loading failed.
    /// </summary>
    public void SetFailed(Exception ex)
    {
        _tcs.TrySetException(ex);
    }
}
