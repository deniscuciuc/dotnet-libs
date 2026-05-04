namespace DenisCuciuc.Platform.MongoDB.Seeding;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SeederAttribute(string name, string? description = null) : Attribute
{
    public string Name { get; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Seeder name is required.", nameof(name))
        : name;

    public string? Description { get; } = description;

    public override string ToString()
    {
        return $"Seeder '{Name}': {Description}";
    }
}
