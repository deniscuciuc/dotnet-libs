using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CoreLibs.LiveConfig;

/// <summary>
/// Central orchestrator for config import, versioning, distribution, and application.
/// Handles the full lifecycle: serialize → hash → compare → store → distribute → hooks → apply.
/// </summary>
public sealed class LiveConfigEngine(
    IConfigStore store,
    ConfigApplierRegistry applierRegistry,
    ConfigHookRegistry hookRegistry,
    ILogger<LiveConfigEngine> logger,
    IConfigDistributor? distributor = null,
    ConfigRetentionOptions? retentionOptions = null)
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Imports data for a config type. Serializes to JSON, computes hash,
    /// creates a new version if changed, distributes, and applies.
    /// </summary>
    public async Task<ConfigImportResult> ImportAsync<T>(
        string configType,
        IEnumerable<T> data,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        var json = ConfigHasher.SerializeToJson(data);
        return await ImportRawAsync(configType, json, createdBy, cancellationToken);
    }

    /// <summary>
    /// Imports raw JSON data for a config type. Computes hash,
    /// creates a new version if changed, distributes, and applies.
    /// </summary>
    public async Task<ConfigImportResult> ImportRawAsync(
        string configType,
        string json,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        var semaphore = _locks.GetOrAdd(configType, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var hash = ConfigHasher.ComputeHash(json);

            var activeInfo = await store.GetActiveVersionInfoAsync(configType, cancellationToken);
            if (activeInfo is not null && string.Equals(activeInfo.DataHash, hash, StringComparison.Ordinal))
            {
                logger.LogInformation("Config {ConfigType} unchanged (hash match), skipping import", configType);
                return new ConfigImportResult(configType, false, activeInfo.Version);
            }

            var version = await store.CreateVersionAsync(configType, json, hash, createdBy, cancellationToken);
            logger.LogInformation("Created config {ConfigType} v{Version}", configType, version);

            await store.ActivateVersionAsync(configType, version, cancellationToken);

            // Run retention cleanup under the lock to prevent races with concurrent imports
            if (retentionOptions is { Enabled: true })
                await RunRetentionCleanupAsync(configType, retentionOptions);

            if (distributor is not null)
            {
                await distributor.SetAsync(configType, json, version, cancellationToken);
                await distributor.PublishUpdateAsync(
                    new ConfigUpdateNotification(configType, version, false), cancellationToken);
            }

            var hookContext = new ConfigHookContext(configType, json, version, false);
            await hookRegistry.InvokeBeforeApplyAsync(hookContext, cancellationToken);
            await applierRegistry.ApplyAsync(configType, json, version, logger, cancellationToken);
            await hookRegistry.InvokeAfterApplyAsync(hookContext, cancellationToken);

            return new ConfigImportResult(configType, true, version);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import config {ConfigType}", configType);
            return new ConfigImportResult(configType, false, 0, ex.Message);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Rolls back a config type to a previous version.
    /// </summary>
    public async Task<ConfigImportResult> RollbackAsync(
        string configType,
        int targetVersion,
        string? requestedBy = null,
        CancellationToken cancellationToken = default)
    {
        var semaphore = _locks.GetOrAdd(configType, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var target = await store.GetVersionAsync(configType, targetVersion, cancellationToken);
            if (target is null)
            {
                logger.LogWarning("Rollback failed: version {Version} not found for {ConfigType}", targetVersion,
                    configType);
                return new ConfigImportResult(configType, false, 0, $"Version {targetVersion} not found");
            }

            var activated = await store.ActivateVersionAsync(configType, targetVersion, cancellationToken);
            if (!activated)
                return new ConfigImportResult(configType, false, targetVersion, "Failed to activate version");

            logger.LogInformation("Rolled back config {ConfigType} to v{Version} by {RequestedBy}",
                configType, targetVersion, requestedBy ?? "unknown");

            if (distributor is not null)
            {
                await distributor.SetAsync(configType, target.DataJson, targetVersion, cancellationToken);
                await distributor.PublishUpdateAsync(
                    new ConfigUpdateNotification(configType, targetVersion, true), cancellationToken);
            }

            var hookContext = new ConfigHookContext(configType, target.DataJson, targetVersion, true);
            await hookRegistry.InvokeBeforeApplyAsync(hookContext, cancellationToken);
            await applierRegistry.ApplyAsync(configType, target.DataJson, targetVersion, logger, cancellationToken);
            await hookRegistry.InvokeAfterApplyAsync(hookContext, cancellationToken);

            return new ConfigImportResult(configType, true, targetVersion);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rollback config {ConfigType} to v{Version}", configType, targetVersion);
            return new ConfigImportResult(configType, false, 0, ex.Message);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Loads and applies a config from the distributor (primary) or store (fallback).
    /// Used during startup and reconciliation.
    /// </summary>
    public async Task<bool> LoadAndApplyAsync(string configType, CancellationToken cancellationToken = default)
    {
        if (distributor is not null)
        {
            var entry = await distributor.GetAsync(configType, cancellationToken);
            if (entry is not null)
            {
                await applierRegistry.ApplyAsync(configType, entry.DataJson, entry.Version, logger, cancellationToken);
                logger.LogDebug("Loaded config {ConfigType} v{Version} from distributor", configType, entry.Version);
                return true;
            }
        }

        var active = await store.GetActiveAsync(configType, cancellationToken);
        if (active is null)
        {
            logger.LogDebug("No active config found for {ConfigType}", configType);
            return false;
        }

        await applierRegistry.ApplyAsync(configType, active.DataJson, active.Version, logger, cancellationToken);
        logger.LogDebug("Loaded config {ConfigType} v{Version} from store", configType, active.Version);

        if (distributor is not null)
            await distributor.SetAsync(configType, active.DataJson, active.Version, cancellationToken);

        return true;
    }

    /// <summary>
    /// Gets the merged manifest from store and distributor.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, int>> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        return await store.GetManifestAsync(cancellationToken);
    }

    /// <summary>
    /// Imports a batch of config snapshots using the specified transaction mode.
    /// <para>
    /// <b>Partial (default):</b> each config imports independently. Failures don't block others.
    /// </para>
    /// <para>
    /// <b>Transactional:</b> all-or-nothing. If any config fails to validate/store,
    /// nothing is applied — staged versions are not activated, distributor is not updated.
    /// </para>
    /// </summary>
    public async Task<ConfigBatchImportResult> ImportBatchAsync(
        IReadOnlyDictionary<string, ConfigSnapshot> snapshots,
        ImportTransactionMode mode = ImportTransactionMode.Partial,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        if (snapshots.Count == 0) return new ConfigBatchImportResult { Results = [], Mode = mode };

        return mode switch
        {
            ImportTransactionMode.Transactional => await ImportBatchTransactionalAsync(snapshots, createdBy,
                cancellationToken),
            _ => await ImportBatchPartialAsync(snapshots, createdBy, cancellationToken)
        };
    }

    /// <summary>
    /// Partial mode: each config imported independently. Failures logged, don't block others.
    /// </summary>
    private async Task<ConfigBatchImportResult> ImportBatchPartialAsync(
        IReadOnlyDictionary<string, ConfigSnapshot> snapshots,
        string? createdBy,
        CancellationToken cancellationToken)
    {
        var results = new List<ConfigImportResult>(snapshots.Count);

        foreach (var (configType, snapshot) in snapshots)
        {
            var result = await ImportRawAsync(configType, snapshot.DataJson, createdBy, cancellationToken);
            results.Add(result);

            if (!result.IsSuccess)
                logger.LogWarning("Partial import: {ConfigType} failed — {Error}. Continuing with remaining configs",
                    configType, result.Error);
        }

        return new ConfigBatchImportResult
        {
            Results = results,
            Mode = ImportTransactionMode.Partial,
            WasRolledBack = false
        };
    }

    /// <summary>
    /// Transactional mode: all-or-nothing.
    /// <list type="number">
    ///   <item>Stage all versions (create but don't activate)</item>
    ///   <item>If any staging fails → return errors, nothing activated</item>
    ///   <item>Activate all staged versions atomically</item>
    ///   <item>Distribute all</item>
    ///   <item>Apply hooks + appliers</item>
    /// </list>
    /// </summary>
    private async Task<ConfigBatchImportResult> ImportBatchTransactionalAsync(
        IReadOnlyDictionary<string, ConfigSnapshot> snapshots,
        string? createdBy,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Transactional import: staging {Count} configs", snapshots.Count);

        // Phase 1: Acquire locks for all config types (sorted to prevent deadlocks)
        var sortedTypes = snapshots.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        var acquiredLocks = new List<(string ConfigType, SemaphoreSlim Semaphore)>();

        try
        {
            foreach (var configType in sortedTypes)
            {
                var semaphore = _locks.GetOrAdd(configType, _ => new SemaphoreSlim(1, 1));
                await semaphore.WaitAsync(cancellationToken);
                acquiredLocks.Add((configType, semaphore));
            }

            // Phase 2: Stage — compute hashes, create versions (but don't activate)
            var staged = new List<StagedImport>(snapshots.Count);
            var stagingErrors = new List<ConfigImportResult>();

            foreach (var configType in sortedTypes)
            {
                var snapshot = snapshots[configType];

                try
                {
                    var hash = ConfigHasher.ComputeHash(snapshot.DataJson);

                    var activeInfo = await store.GetActiveVersionInfoAsync(configType, cancellationToken);
                    if (activeInfo is not null &&
                        string.Equals(activeInfo.DataHash, hash, StringComparison.Ordinal))
                    {
                        staged.Add(new StagedImport(configType, snapshot.DataJson, hash, activeInfo.Version, false));
                        continue;
                    }

                    var version = await store.CreateVersionAsync(configType, snapshot.DataJson, hash, createdBy,
                        cancellationToken);
                    staged.Add(new StagedImport(configType, snapshot.DataJson, hash, version, true));

                    logger.LogDebug("Transactional import: staged {ConfigType} v{Version}", configType, version);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Transactional import: staging failed for {ConfigType}", configType);
                    stagingErrors.Add(new ConfigImportResult(configType, false, 0, ex.Message));
                }
            }

            if (stagingErrors.Count > 0)
            {
                logger.LogWarning("Transactional import: ABORTED — {FailCount}/{Total} configs failed staging",
                    stagingErrors.Count, snapshots.Count);

                var abortedResults = staged
                    .Select(s => new ConfigImportResult(s.ConfigType, false, s.Version,
                        "Aborted: another config in the batch failed"))
                    .Concat(stagingErrors)
                    .ToList();

                return new ConfigBatchImportResult
                {
                    Results = abortedResults,
                    Mode = ImportTransactionMode.Transactional,
                    WasRolledBack = true
                };
            }

            // Phase 3: Commit — activate all staged versions with compensation on failure
            var results = new List<ConfigImportResult>(staged.Count);
            var committed = new List<StagedImport>();

            foreach (var s in staged)
            {
                if (!s.Changed)
                {
                    results.Add(new ConfigImportResult(s.ConfigType, false, s.Version));
                    continue;
                }

                try
                {
                    await store.ActivateVersionAsync(s.ConfigType, s.Version, cancellationToken);
                    committed.Add(s);

                    if (distributor is not null)
                    {
                        await distributor.SetAsync(s.ConfigType, s.DataJson, s.Version, cancellationToken);
                        await distributor.PublishUpdateAsync(
                            new ConfigUpdateNotification(s.ConfigType, s.Version, false), cancellationToken);
                    }

                    var hookContext = new ConfigHookContext(s.ConfigType, s.DataJson, s.Version, false);
                    await hookRegistry.InvokeBeforeApplyAsync(hookContext, cancellationToken);
                    await applierRegistry.ApplyAsync(s.ConfigType, s.DataJson, s.Version, logger, cancellationToken);
                    await hookRegistry.InvokeAfterApplyAsync(hookContext, cancellationToken);

                    results.Add(new ConfigImportResult(s.ConfigType, true, s.Version));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Transactional import: activation failed for {ConfigType} v{Version}",
                        s.ConfigType, s.Version);
                    results.Add(new ConfigImportResult(s.ConfigType, false, s.Version, ex.Message));

                    // Compensate: roll back previously committed configs to their prior versions
                    await CompensateCommittedAsync(committed, cancellationToken);

                    // Mark all remaining staged configs as failed
                    var remaining = staged
                        .Where(r => r.Changed && !committed.Contains(r) && r != s)
                        .Select(r => new ConfigImportResult(r.ConfigType, false, r.Version,
                            "Aborted: compensation triggered by failure in batch"));
                    results.AddRange(remaining);

                    return new ConfigBatchImportResult
                    {
                        Results = results,
                        Mode = ImportTransactionMode.Transactional,
                        WasRolledBack = true
                    };
                }
            }

            logger.LogInformation("Transactional import: COMMITTED {Changed}/{Total} configs ({Unchanged} unchanged)",
                results.Count(r => r.Changed), results.Count, results.Count(r => r.IsSuccess && !r.Changed));

            return new ConfigBatchImportResult
            {
                Results = results,
                Mode = ImportTransactionMode.Transactional,
                WasRolledBack = false
            };
        }
        finally
        {
            // Release all acquired locks (reverse order)
            for (var i = acquiredLocks.Count - 1; i >= 0; i--) acquiredLocks[i].Semaphore.Release();
        }
    }

    private sealed record StagedImport(string ConfigType, string DataJson, string DataHash, int Version, bool Changed);

    /// <summary>
    /// Compensates previously committed configs by reactivating their prior versions.
    /// Best-effort: failures are logged as critical but do not throw.
    /// </summary>
    private async Task CompensateCommittedAsync(
        List<StagedImport> committed,
        CancellationToken cancellationToken)
    {
        foreach (var c in committed)
        {
            try
            {
                var previousVersion = c.Version - 1;
                if (previousVersion <= 0)
                {
                    logger.LogWarning("Compensation: no prior version to restore for {ConfigType}, skipping",
                        c.ConfigType);
                    continue;
                }

                var previous = await store.GetVersionAsync(c.ConfigType, previousVersion, cancellationToken);
                if (previous is null)
                {
                    logger.LogCritical(
                        "Compensation FAILED: prior version {Version} not found for {ConfigType}. Manual intervention required",
                        previousVersion, c.ConfigType);
                    continue;
                }

                await store.ActivateVersionAsync(c.ConfigType, previousVersion, cancellationToken);

                if (distributor is not null)
                {
                    await distributor.SetAsync(c.ConfigType, previous.DataJson, previousVersion, cancellationToken);
                    await distributor.PublishUpdateAsync(
                        new ConfigUpdateNotification(c.ConfigType, previousVersion, true), cancellationToken);
                }

                await applierRegistry.ApplyAsync(c.ConfigType, previous.DataJson, previousVersion, logger,
                    cancellationToken);

                logger.LogWarning("Compensation: rolled back {ConfigType} from v{NewVersion} to v{OldVersion}",
                    c.ConfigType, c.Version, previousVersion);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex,
                    "Compensation FAILED for {ConfigType} v{Version}. System may be in inconsistent state. Manual intervention required",
                    c.ConfigType, c.Version);
            }
        }
    }

    /// <summary>
    /// Runs retention cleanup asynchronously. Failures are logged but do not propagate
    /// because cleanup is best-effort and must not block the import pipeline.
    /// </summary>
    private async Task RunRetentionCleanupAsync(string configType, ConfigRetentionOptions retention)
    {
        try
        {
            var deleted = await store.CleanupOldVersionsAsync(configType, retention);
            if (deleted > 0)
                logger.LogDebug("Retention cleanup for {ConfigType}: removed {DeletedCount} old version(s)",
                    configType, deleted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Retention cleanup failed for {ConfigType} — will retry on next import", configType);
        }
    }
}
