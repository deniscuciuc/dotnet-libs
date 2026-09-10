namespace CoreLibs.Identity.Settings;

public sealed class AnonymousAuthOptions
{
    public const string DefaultSectionPath = "Identity:Anonymous";

    public bool IsAllowAnonymous { get; set; }

    public int LoginLimitFromIpInHour { get; set; } = 30;

    public string JwtSecretBase64 { get; set; } = null!;

    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    public int ExpirationInMinutes { get; set; } = 60;
}
