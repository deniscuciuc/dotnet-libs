using CoreLibs.LiveConfig.Hosting.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig.Examples.Worker.Consumers;

/// <summary>
/// Listens for <see cref="ConfigImportCompleted"/> events published by the import consumer
/// and records them in the <see cref="QueueResultTracker"/> for the example dashboard.
/// </summary>
public sealed class ConfigImportCompletedConsumer(
    QueueResultTracker tracker,
    ILogger<ConfigImportCompletedConsumer> logger) : IConsumer<ConfigImportCompleted>
{
    public Task Consume(ConsumeContext<ConfigImportCompleted> context)
    {
        var msg = context.Message;
        tracker.RecordImport(msg);

        var changed = msg.Results.Count(r => r.Changed);
        var failed = msg.Results.Count(r => !r.IsSuccess);

        logger.LogInformation(
            "[Queue] Import completed — {Total} config(s): {Changed} changed, {Failed} failed. RequestedBy: {RequestedBy}",
            msg.Results.Count, changed, failed, msg.RequestedBy ?? "(unknown)");

        return Task.CompletedTask;
    }
}

/// <summary>
/// Listens for <see cref="ConfigRollbackCompleted"/> events published by the rollback consumer
/// and records them in the <see cref="QueueResultTracker"/> for the example dashboard.
/// </summary>
public sealed class ConfigRollbackCompletedConsumer(
    QueueResultTracker tracker,
    ILogger<ConfigRollbackCompletedConsumer> logger) : IConsumer<ConfigRollbackCompleted>
{
    public Task Consume(ConsumeContext<ConfigRollbackCompleted> context)
    {
        var msg = context.Message;
        tracker.RecordRollback(msg);

        if (msg.Success)
            logger.LogInformation(
                "[Queue] Rollback completed — {ConfigType} → v{Version}. RequestedBy: {RequestedBy}",
                msg.ConfigType, msg.Version, msg.RequestedBy ?? "(unknown)");
        else
            logger.LogWarning(
                "[Queue] Rollback failed — {ConfigType} → v{Version}: {Error}. RequestedBy: {RequestedBy}",
                msg.ConfigType, msg.Version, msg.Error, msg.RequestedBy ?? "(unknown)");

        return Task.CompletedTask;
    }
}
