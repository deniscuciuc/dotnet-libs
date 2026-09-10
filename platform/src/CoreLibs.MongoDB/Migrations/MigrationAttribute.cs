namespace CoreLibs.MongoDB.Migrations;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MigrationAttribute : Attribute
{
    public MigrationAttribute(int version, string? description = null)
    {
        if (version <= 0)
            throw new ArgumentOutOfRangeException(nameof(version), "Migration version must be a positive integer.");
        Version = version;
        Description = description;
    }

    public int Version { get; }
    public string? Description { get; }

    public override string ToString()
    {
        return $"Migration v{Version}: {Description}";
    }
}
