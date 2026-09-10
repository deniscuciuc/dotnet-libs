using System.ComponentModel.DataAnnotations;

namespace CoreLibs.Redis;

public sealed class RedisOptions
{
    public const string DefaultSectionPath = "Redis";

    [Required]
    public string ConnectionString { get; set; } = "localhost:6379,resolveDns=true";
}
