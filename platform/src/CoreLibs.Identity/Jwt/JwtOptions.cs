namespace CoreLibs.Identity.Jwt;

public sealed class JwtOptions
{
    public string Key { get; set; } = null!;

    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(30);
}
