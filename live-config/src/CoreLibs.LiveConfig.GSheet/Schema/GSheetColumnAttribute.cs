namespace CoreLibs.LiveConfig.GSheet.Schema;

/// <summary>
/// Maps a row property to a Google Sheet column by header name.
/// When applied with a name, the auto-mapper uses that name instead of the property name.
/// When applied without a name, marks the property for inclusion using its property name.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class GSheetColumnAttribute : Attribute
{
    /// <summary>
    /// Creates a column mapping using the property name as the column header.
    /// </summary>
    public GSheetColumnAttribute()
    {
    }

    /// <summary>
    /// Creates a column mapping using the specified column header name.
    /// </summary>
    public GSheetColumnAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// The column header name in the Google Sheet.
    /// When <c>null</c>, the property name is used.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Optional column order (0-based) for positional matching when headers are absent.
    /// Default is -1 (use name-based matching).
    /// </summary>
    public int Order { get; set; } = -1;
}
