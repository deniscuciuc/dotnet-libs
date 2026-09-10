using System.Collections.Concurrent;
using CoreLibs.LiveConfig.Hosting.Contracts;

namespace CoreLibs.LiveConfig.Examples.Worker;

/// <summary>
/// In-memory tracker that stores recent import/rollback results received via MQ.
/// Used by the example to demonstrate the queue-based flow end-to-end.
/// </summary>
public sealed class QueueResultTracker
{
    private readonly ConcurrentQueue<ImportEntry> _imports = new();
    private readonly ConcurrentQueue<RollbackEntry> _rollbacks = new();

    private const int MaxEntries = 50;

    public void RecordImport(ConfigImportCompleted completed)
    {
        _imports.Enqueue(new ImportEntry(completed, DateTimeOffset.UtcNow));
        while (_imports.Count > MaxEntries) _imports.TryDequeue(out _);
    }

    public void RecordRollback(ConfigRollbackCompleted completed)
    {
        _rollbacks.Enqueue(new RollbackEntry(completed, DateTimeOffset.UtcNow));
        while (_rollbacks.Count > MaxEntries) _rollbacks.TryDequeue(out _);
    }

    public IReadOnlyList<ImportEntry> RecentImports => _imports.Reverse().ToList();
    public IReadOnlyList<RollbackEntry> RecentRollbacks => _rollbacks.Reverse().ToList();

    public sealed record ImportEntry(ConfigImportCompleted Result, DateTimeOffset ReceivedAt);
    public sealed record RollbackEntry(ConfigRollbackCompleted Result, DateTimeOffset ReceivedAt);
}
