namespace DenisCuciuc.Platform.Identity.Settings;

public sealed class ApiTokenAuthOptions
{
    public const string DefaultSectionPath = "Identity:ApiToken";

    public bool IsAllowed { get; set; }

    public string Issuer { get; set; } = null!;

    public int ExpirationInMinutes { get; set; }

    public Dictionary<string, string> ActiveKeys { get; set; } = [];
}
