namespace CoreLibs.Identity.Settings;

public sealed class ApiTokenAuthOptions
{
    public const string DefaultSectionPath = "Identity:ApiToken";

    public bool IsAllowed { get; set; }

    public string Issuer { get; set; } = null!;

    public int ExpirationInMinutes { get; set; }

    /// <summary>
    /// Audience the incoming token must carry. <c>{environment}</c> is replaced with the
    /// current environment name. Defaults to the historical <c>Service.Api.{environment}</c>.
    /// </summary>
    public string AudienceFormat { get; set; } = "Service.Api.{environment}";

    public Dictionary<string, string> ActiveKeys { get; set; } = [];
}
