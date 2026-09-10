namespace CoreLibs.LiveConfig;

/// <summary>
/// Lifecycle hooks invoked before and after config application.
/// </summary>
public interface IConfigHook
{
    /// <summary>
    /// Called before a config is applied to all appliers.
    /// </summary>
    Task OnBeforeApplyAsync(ConfigHookContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after a config has been applied to all appliers.
    /// </summary>
    Task OnAfterApplyAsync(ConfigHookContext context, CancellationToken cancellationToken = default);
}
