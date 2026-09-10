namespace CoreLibs.LiveConfig.GSheet.Pipeline;

/// <summary>
/// Aggregates multiple rows into domain entities. Critical for game configs where
/// multiple rows represent a single entity (e.g., quest with multiple rewards).
/// <para>
/// Output must be deterministically ordered for stable hash-based versioning.
/// </para>
/// </summary>
/// <typeparam name="TRow">The parsed row type.</typeparam>
/// <typeparam name="TDomain">The domain model type.</typeparam>
public interface IAggregator<in TRow, out TDomain>
{
    /// <summary>
    /// Groups and transforms rows into domain entities (1:N, N:1, many-to-many).
    /// </summary>
    IEnumerable<TDomain> Aggregate(IReadOnlyList<TRow> rows, GSheetImportContext context);
}
