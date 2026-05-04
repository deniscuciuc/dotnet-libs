namespace DenisCuciuc.Platform.Identity.Settings;

public sealed class TelegramAuthOptions
{
    public const string DefaultSectionPath = "Identity:Telegram";

    public string JwtSecretBase64 { get; set; } = null!;

    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    public int ExpirationInMinutes { get; set; }
}
