using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CoreLibs.LiveConfig.Store.Postgres;

/// <summary>
/// Postgres implementation of <see cref="IConfigStore"/> using raw Npgsql and Dapper.
/// </summary>
public sealed class PostgresConfigStore(
    NpgsqlDataSource dataSource,
    IOptions<PostgresConfigStoreOptions> options,
    ILogger<PostgresConfigStore> logger) : IConfigStore
{
    private readonly string _table = $"{options.Value.Schema}.{options.Value.TableName}";

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS {_table} (
                id          BIGSERIAL PRIMARY KEY,
                config_type TEXT        NOT NULL,
                version     INT         NOT NULL,
                data_json   TEXT        NOT NULL,
                data_hash   TEXT        NOT NULL,
                is_active   BOOLEAN     NOT NULL DEFAULT FALSE,
                created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                created_by  TEXT,
                CONSTRAINT uq_{options.Value.TableName}_type_version UNIQUE (config_type, version)
            )
            """);
    }

    public async Task<ConfigVersion?> GetActiveAsync(string configType, CancellationToken cancellationToken = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleOrDefaultAsync<Row>(
            $"SELECT * FROM {_table} WHERE config_type = @ConfigType AND is_active = TRUE LIMIT 1",
            new { ConfigType = configType });
        return row is null ? null : ToConfigVersion(row);
    }

    public async Task<ConfigVersion?> GetVersionAsync(string configType, int version,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleOrDefaultAsync<Row>(
            $"SELECT * FROM {_table} WHERE config_type = @ConfigType AND version = @Version LIMIT 1",
            new { ConfigType = configType, Version = version });
        return row is null ? null : ToConfigVersion(row);
    }

    public async Task<IReadOnlyList<ConfigVersion>> GetRecentVersionsAsync(string configType, int count = 10,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<Row>(
            $"SELECT * FROM {_table} WHERE config_type = @ConfigType ORDER BY version DESC LIMIT @Count",
            new { ConfigType = configType, Count = count });
        return rows.Select(ToConfigVersion).ToList();
    }

    public async Task<int> GetLatestVersionNumberAsync(string configType, CancellationToken cancellationToken = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        var version = await conn.ExecuteScalarAsync<int?>(
            $"SELECT MAX(version) FROM {_table} WHERE config_type = @ConfigType",
            new { ConfigType = configType });
        return version ?? 0;
    }

    public async Task<string?> GetActiveHashAsync(string configType, CancellationToken cancellationToken = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<string?>(
            $"SELECT data_hash FROM {_table} WHERE config_type = @ConfigType AND is_active = TRUE LIMIT 1",
            new { ConfigType = configType });
    }

    public async Task<ActiveVersionInfo?> GetActiveVersionInfoAsync(string configType,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleOrDefaultAsync<(string data_hash, int version)>(
            $"SELECT data_hash, version FROM {_table} WHERE config_type = @ConfigType AND is_active = TRUE LIMIT 1",
            new { ConfigType = configType });
        return row == default ? null : new ActiveVersionInfo(row.data_hash, row.version);
    }

    public async Task<int> CreateVersionAsync(string configType, string dataJson, string dataHash,
        string? createdBy = null, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
            try
            {
                var newVersion = await conn.ExecuteScalarAsync<int>($"""
                    INSERT INTO {_table} (config_type, version, data_json, data_hash, is_active, created_at, created_by)
                    VALUES (@ConfigType,
                            (SELECT COALESCE(MAX(version), 0) + 1 FROM {_table} WHERE config_type = @ConfigType),
                            @DataJson, @DataHash, FALSE, NOW(), @CreatedBy)
                    RETURNING version
                    """,
                    new { ConfigType = configType, DataJson = dataJson, DataHash = dataHash, CreatedBy = createdBy });
                return newVersion;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && attempt < maxRetries - 1)
            {
                logger.LogWarning(
                    "Duplicate key on create version for {ConfigType}, retrying (attempt {Attempt})",
                    configType, attempt + 1);
            }
        }

        throw new InvalidOperationException($"Failed to create version for {configType} after {maxRetries} attempts");
    }

    public async Task<bool> ActivateVersionAsync(string configType, int version,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        await conn.ExecuteAsync(
            $"UPDATE {_table} SET is_active = FALSE WHERE config_type = @ConfigType AND is_active = TRUE",
            new { ConfigType = configType }, transaction: tx);
        var updated = await conn.ExecuteAsync(
            $"UPDATE {_table} SET is_active = TRUE WHERE config_type = @ConfigType AND version = @Version",
            new { ConfigType = configType, Version = version }, transaction: tx);
        await tx.CommitAsync(cancellationToken);
        return updated > 0;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<(string config_type, int version)>(
            $"SELECT config_type, version FROM {_table} WHERE is_active = TRUE");
        return rows.ToDictionary(r => r.config_type, r => r.version);
    }

    public async Task<int> CleanupOldVersionsAsync(string configType, ConfigRetentionOptions retention,
        CancellationToken cancellationToken = default)
    {
        if (!retention.Enabled || (retention.MaxVersions <= 0 && retention.MaxAge is null))
            return 0;

        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);

        var activeFilter = retention.KeepActiveAlways ? "AND is_active = FALSE" : "";
        var cutoffDate = retention.MaxAge is not null ? (DateTime?)(DateTime.UtcNow - retention.MaxAge.Value) : null;

        var keepConditions = new List<string>();
        if (retention.MaxVersions > 0)
            keepConditions.Add($"version IN (SELECT version FROM {_table} WHERE config_type = @ConfigType {activeFilter} ORDER BY version DESC LIMIT @MaxVersions)");
        if (cutoffDate is not null)
            keepConditions.Add("created_at >= @CutoffDate");

        if (keepConditions.Count == 0)
            return 0;

        var deleted = await conn.ExecuteAsync($"""
            DELETE FROM {_table}
            WHERE config_type = @ConfigType {activeFilter}
              AND id NOT IN (
                  SELECT id FROM {_table}
                  WHERE config_type = @ConfigType {activeFilter}
                    AND ({string.Join(" OR ", keepConditions)})
              )
            """,
            new { ConfigType = configType, MaxVersions = retention.MaxVersions, CutoffDate = cutoffDate });

        logger.LogInformation(
            "Retention cleanup for {ConfigType}: deleted {DeletedCount} old version(s)",
            configType, deleted);

        return deleted;
    }

    private static ConfigVersion ToConfigVersion(Row r) =>
        new(r.config_type, r.version, r.data_json, r.data_hash, r.is_active,
            new DateTimeOffset(r.created_at, TimeSpan.Zero), r.created_by);

    private sealed record Row(
        long id, string config_type, int version, string data_json,
        string data_hash, bool is_active, DateTime created_at, string? created_by);
}
