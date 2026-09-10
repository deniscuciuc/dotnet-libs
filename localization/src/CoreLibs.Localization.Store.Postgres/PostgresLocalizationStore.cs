using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CoreLibs.Localization.Store.Postgres;

/// <summary>
/// PostgreSQL implementation of <see cref="ILocalizationStore"/> using raw Npgsql and Dapper.
/// </summary>
public sealed class PostgresLocalizationStore(
    NpgsqlDataSource dataSource,
    IOptions<PostgresLocalizationStoreOptions> options) : ILocalizationStore
{
    private readonly string _table = $"{options.Value.Schema}.{options.Value.TableName}";

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await conn.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS {_table} (
                culture TEXT NOT NULL,
                key     TEXT NOT NULL,
                value   TEXT NOT NULL,
                PRIMARY KEY (culture, key)
            )
            """);
    }

    public Task EnsureCreatedAsync(CancellationToken ct = default) => EnsureSchemaAsync(ct);

    public async Task<IEnumerable<LocalizationEntry>> FindAllAsync(
        string culture, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<(string culture, string key, string value)>(
            $"SELECT culture, key, value FROM {_table} WHERE culture = @Culture",
            new { Culture = culture });
        return rows.Select(r => new LocalizationEntry
        {
            Key = r.key,
            Culture = r.culture,
            Value = JsonSerializer.Deserialize<LocalizationValue>(r.value, JsonSerializerOptions.Web)!
        });
    }

    public async Task UpsertAsync(LocalizationEntry entry, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var json = JsonSerializer.Serialize(entry.Value, JsonSerializerOptions.Web);
        await conn.ExecuteAsync($"""
            INSERT INTO {_table} (culture, key, value)
            VALUES (@Culture, @Key, @Value)
            ON CONFLICT (culture, key) DO UPDATE SET value = EXCLUDED.value
            """,
            new { Culture = entry.Culture, Key = entry.Key, Value = json });
    }

    public async Task UpsertManyAsync(IEnumerable<LocalizationEntry> entries, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        foreach (var entry in entries)
        {
            var json = JsonSerializer.Serialize(entry.Value, JsonSerializerOptions.Web);
            await conn.ExecuteAsync($"""
                INSERT INTO {_table} (culture, key, value)
                VALUES (@Culture, @Key, @Value)
                ON CONFLICT (culture, key) DO UPDATE SET value = EXCLUDED.value
                """,
                new { Culture = entry.Culture, Key = entry.Key, Value = json },
                transaction: tx);
        }
        await tx.CommitAsync(ct);
    }

    public async Task DeleteAsync(string key, string culture, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            $"DELETE FROM {_table} WHERE culture = @Culture AND key = @Key",
            new { Culture = culture, Key = key });
    }

    public async Task<bool> ExistsAsync(string key, string culture, CancellationToken ct = default)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<bool>(
            $"SELECT EXISTS(SELECT 1 FROM {_table} WHERE culture = @Culture AND key = @Key)",
            new { Culture = culture, Key = key });
    }
}
