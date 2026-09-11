using System.Diagnostics;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.CQRS;

/// <summary>
/// MediatR pipeline behavior that logs request execution, duration, and outcome.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResult>(
    ILogger<LoggingBehavior<TRequest, TResult>> logger,
    IOptionsMonitor<CoreCqrsOptions> optionsMonitor)
    : IPipelineBehavior<TRequest, ErrorOr<TResult>>
    where TRequest : IRequest<ErrorOr<TResult>>
{
    public async Task<ErrorOr<TResult>> Handle(
        TRequest request,
        RequestHandlerDelegate<ErrorOr<TResult>> next,
        CancellationToken cancellationToken)
    {
        var options = optionsMonitor.CurrentValue;
        if (!options.EnableLoggingBehavior)
            return await next();

        var requestName = typeof(TRequest).Name;
        var traceId = Activity.Current?.TraceId.ToString();

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CqrsRequest"] = requestName,
            ["TraceId"] = traceId
        });

        logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (response.IsError)
        {
            if (options.EnableDetailedErrorLogging)
            {
                logger.LogWarning(
                    "Handled {RequestName} with errors in {ElapsedMs}ms: {@Errors}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    response.Errors);
            }
            else
            {
                logger.LogWarning(
                    "Handled {RequestName} with {ErrorCount} error(s) in {ElapsedMs}ms",
                    requestName,
                    response.Errors.Count,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        else
        {
            logger.LogInformation(
                "Handled {RequestName} successfully in {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
