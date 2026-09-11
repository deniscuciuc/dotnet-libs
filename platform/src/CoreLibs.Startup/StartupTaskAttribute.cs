namespace CoreLibs.Startup;

/// <summary>
/// Marks an <see cref="IStartup"/> implementation for auto-registration via
/// <see cref="StartupExtensions.AddCoreStartupTasksFromAssemblyOf{T}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class StartupTaskAttribute : Attribute
{
    /// <summary>Execution order. Lower values run first. Default is <c>0</c>.</summary>
    public int Order { get; init; }

    /// <summary>
    /// When non-empty, the task only executes in the listed environments.
    /// An empty array (default) means the task runs in every environment.
    /// </summary>
    public string[] Environments { get; init; } = [];
}
