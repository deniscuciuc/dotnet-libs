namespace CoreLibs.LiveConfig.GSheet.Entity;

/// <summary>
/// Metadata about a registered entity adapter. Used by the orchestrator to build execution plans.
/// </summary>
internal sealed record EntityRegistration(
    Type AdapterType,
    string SheetName,
    string Range,
    Type[] Needs);
