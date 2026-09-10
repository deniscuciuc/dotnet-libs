namespace CoreLibs.Postgres;

/// <summary>
/// Controls the EF Core naming convention applied to table and column names.
/// Requires the <c>EFCore.NamingConventions</c> package.
/// </summary>
public enum PostgresNamingConvention
{
    /// <summary>No naming convention is applied; EF Core defaults are used.</summary>
    None,

    /// <summary>
    /// Converts all names to snake_case (e.g. <c>DriveItem</c> â†’ <c>drive_item</c>).
    /// This is the default.
    /// </summary>
    SnakeCase,

    /// <summary>Converts all names to lowercase (e.g. <c>DriveItem</c> â†’ <c>driveitem</c>).</summary>
    LowerCase,

    /// <summary>Converts all names to camelCase (e.g. <c>DriveItem</c> â†’ <c>driveItem</c>).</summary>
    CamelCase,
}
