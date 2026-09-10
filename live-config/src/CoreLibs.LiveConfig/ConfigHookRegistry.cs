namespace CoreLibs.LiveConfig;

/// <summary>
/// Registry for config lifecycle hooks. Hooks are invoked in registration order.
/// </summary>
public sealed class ConfigHookRegistry
{
    private readonly List<IConfigHook> _hooks = [];

    public void Register(IConfigHook hook)
    {
        _hooks.Add(hook);
    }

    public IReadOnlyList<IConfigHook> All => _hooks;

    public async Task InvokeBeforeApplyAsync(ConfigHookContext context, CancellationToken cancellationToken = default)
    {
        foreach (var hook in _hooks) await hook.OnBeforeApplyAsync(context, cancellationToken);
    }

    public async Task InvokeAfterApplyAsync(ConfigHookContext context, CancellationToken cancellationToken = default)
    {
        foreach (var hook in _hooks) await hook.OnAfterApplyAsync(context, cancellationToken);
    }
}
