namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Observes pipeline execution stages. Register via <c>AddGSheetPipelineObserver&lt;T&gt;()</c>.
/// All methods have default no-op implementations — override only what you need.
/// </summary>
public interface IGSheetPipelineObserver
{
    /// <summary>Called before rows are parsed from the sheet.</summary>
    void OnBeforeParse(string pipelineName, string sheet, string range)
    {
    }

    /// <summary>Called after rows are parsed.</summary>
    void OnAfterParse(string pipelineName, int rowCount)
    {
    }

    /// <summary>Called before validation (attribute + custom row + graph).</summary>
    void OnBeforeValidation(string pipelineName, int rowCount)
    {
    }

    /// <summary>Called after all validation completes.</summary>
    void OnAfterValidation(string pipelineName, int errorCount)
    {
    }

    /// <summary>Called before mapping rows to domain models.</summary>
    void OnBeforeMapping(string pipelineName, int rowCount)
    {
    }

    /// <summary>Called after mapping completes.</summary>
    void OnAfterMapping(string pipelineName, int domainCount)
    {
    }

    /// <summary>Called when the pipeline completes (success or failure).</summary>
    void OnCompleted(string pipelineName, TimeSpan elapsed, bool success)
    {
    }
}
