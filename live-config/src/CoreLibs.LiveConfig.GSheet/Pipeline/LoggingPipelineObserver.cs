using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Default pipeline observer that logs stage transitions with timing.
/// </summary>
public sealed class LoggingPipelineObserver(ILogger<LoggingPipelineObserver> logger) : IGSheetPipelineObserver
{
    private readonly Stopwatch _stageTimer = new();

    public void OnBeforeParse(string pipelineName, string sheet, string range)
    {
        _stageTimer.Restart();
        logger.LogDebug("[{Pipeline}] Parsing sheet '{Sheet}' range '{Range}'", pipelineName, sheet, range);
    }

    public void OnAfterParse(string pipelineName, int rowCount)
    {
        logger.LogDebug("[{Pipeline}] Parsed {RowCount} rows in {ElapsedMs}ms",
            pipelineName, rowCount, _stageTimer.ElapsedMilliseconds);
    }

    public void OnBeforeValidation(string pipelineName, int rowCount)
    {
        _stageTimer.Restart();
        logger.LogDebug("[{Pipeline}] Validating {RowCount} rows", pipelineName, rowCount);
    }

    public void OnAfterValidation(string pipelineName, int errorCount)
    {
        logger.LogDebug("[{Pipeline}] Validation completed in {ElapsedMs}ms — {ErrorCount} errors",
            pipelineName, _stageTimer.ElapsedMilliseconds, errorCount);
    }

    public void OnBeforeMapping(string pipelineName, int rowCount)
    {
        _stageTimer.Restart();
        logger.LogDebug("[{Pipeline}] Mapping {RowCount} rows to domain", pipelineName, rowCount);
    }

    public void OnAfterMapping(string pipelineName, int domainCount)
    {
        logger.LogDebug("[{Pipeline}] Mapped {DomainCount} entities in {ElapsedMs}ms",
            pipelineName, domainCount, _stageTimer.ElapsedMilliseconds);
    }

    public void OnCompleted(string pipelineName, TimeSpan elapsed, bool success)
    {
        if (success)
            logger.LogInformation("[{Pipeline}] Completed in {ElapsedMs}ms", pipelineName, elapsed.TotalMilliseconds);
        else
            logger.LogWarning("[{Pipeline}] Completed with errors in {ElapsedMs}ms", pipelineName,
                elapsed.TotalMilliseconds);
    }
}
