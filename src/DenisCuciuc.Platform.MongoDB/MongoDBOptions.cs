using System.ComponentModel.DataAnnotations;

namespace DenisCuciuc.Platform.MongoDB;

public sealed class MongoDBOptions
{
    public const string DefaultSectionPath = "MongoDB";

    [Required]
    public string ConnectionString { get; set; } = "mongodb://localhost:27017/mydb";

    [Range(0, 10_000)]
    public int MinConnectionPoolSize { get; set; } = 50;

    [Range(1, 10_000)]
    public int MaxConnectionPoolSize { get; set; } = 500;

    public TimeSpan WaitQueueTimeout { get; set; } = TimeSpan.FromMinutes(2);
}
