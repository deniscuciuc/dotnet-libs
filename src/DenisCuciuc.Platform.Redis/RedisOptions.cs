using System.ComponentModel.DataAnnotations;

namespace DenisCuciuc.Platform.Redis;

public sealed class RedisOptions
{
    public const string DefaultSectionPath = "Redis";

    [Required]
    public string ConnectionString { get; set; } = "localhost:6379,resolveDns=true";
}
