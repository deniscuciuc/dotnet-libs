using System.ComponentModel.DataAnnotations;

namespace CoreLibs.Postgres;

public sealed class PostgresOptions
{
    public const string DefaultSectionPath = "Postgres";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(0, 10)]
    public int MaxRetryCount { get; set; } = 3;

    [Range(1, 300)]
    public int CommandTimeout { get; set; } = 30;

    public bool EnableSensitiveDataLogging { get; set; }

    public bool EnableDetailedErrors { get; set; }

    /// <summary>
    /// Enables Npgsql dynamic JSON serialization (via <c>NpgsqlDataSourceBuilder.EnableDynamicJson</c>).
    /// Required when storing dynamic/open-ended types such as <c>Dictionary&lt;string, string&gt;</c>
    /// or <c>object</c> in JSONB/JSON columns.
    /// </summary>
    public bool EnableDynamicJson { get; set; }

    /// <summary>
    /// Naming convention applied to all EF Core table and column names.
    /// Defaults to <see cref="PostgresNamingConvention.SnakeCase"/>.
    /// </summary>
    public PostgresNamingConvention NamingConvention { get; set; } = PostgresNamingConvention.SnakeCase;
}
