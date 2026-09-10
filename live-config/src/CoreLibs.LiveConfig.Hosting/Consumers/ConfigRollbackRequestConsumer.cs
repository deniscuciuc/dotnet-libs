using CoreLibs.LiveConfig.Hosting.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig.Hosting.Consumers;

/// <summary>
/// MassTransit consumer that handles config rollback requests.
/// </summary>
public sealed class ConfigRollbackRequestConsumer(
    LiveConfigEngine engine,
    ILogger<ConfigRollbackRequestConsumer> logger) : IConsumer<ConfigRollbackRequest>
{
    public async Task Consume(ConsumeContext<ConfigRollbackRequest> context)
    {
        var request = context.Message;
        var ct = context.CancellationToken;

        logger.LogInformation(
            "Received config rollback request for {ConfigType} to version {Version}, requested by {RequestedBy}",
            request.ConfigType, request.TargetVersion?.ToString() ?? "(previous)", request.RequestedBy);

        try
        {
            var targetVersion = request.TargetVersion
                                ?? throw new ArgumentException("TargetVersion is required for rollback");
            var result = await engine.RollbackAsync(request.ConfigType, targetVersion, request.RequestedBy, ct);

            var completed = new ConfigRollbackCompleted
            {
                ConfigType = request.ConfigType,
                Version = targetVersion,
                Success = result.Changed,
                RequestedBy = request.RequestedBy,
                CompletedAt = DateTimeOffset.UtcNow
            };

            await context.Publish(completed, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rollback failed for {ConfigType}", request.ConfigType);

            await context.Publish(new ConfigRollbackCompleted
            {
                ConfigType = request.ConfigType,
                Version = request.TargetVersion ?? 0,
                Success = false,
                Error = ex.Message,
                RequestedBy = request.RequestedBy,
                CompletedAt = DateTimeOffset.UtcNow
            }, ct);
        }
    }
}
