using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace CoreLibs.Jobs.Quartz;

/// <summary>
/// Quartz <see cref="IJob"/> adapter that delegates to an <see cref="ICoreJob"/> resolved from DI.
/// </summary>
/// <remarks>
/// The generic type parameter is the concrete <see cref="ICoreJob"/> to execute.
/// Quartz resolves this adapter via DI; the adapter creates a scope and resolves the real job.
/// </remarks>
public sealed class QuartzJobAdapter<TJob>(IServiceScopeFactory scopeFactory) : IJob
    where TJob : ICoreJob
{
    public const string ArgsKey = "CoreLibs.Jobs.Args";

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TJob>();

        if (context.MergedJobDataMap.TryGetString(ArgsKey, out var argsJson)
            && argsJson is not null
            && job is ICoreJob<object> typedJob)
            typedJob.Args = JsonSerializer.Deserialize<object>(argsJson);

        await job.ExecuteAsync(context.CancellationToken);
    }
}
