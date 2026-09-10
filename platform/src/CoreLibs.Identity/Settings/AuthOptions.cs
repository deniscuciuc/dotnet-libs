namespace CoreLibs.Identity.Settings;

public sealed class Auth0Options
{
    public const string DefaultSectionPath = "Identity:Auth0";

    public string Domain { get; set; } = null!;

    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;

    public string ApiAudience { get; set; } = null!;
}
