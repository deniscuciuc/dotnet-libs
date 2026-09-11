using Microsoft.AspNetCore.Authentication;

namespace CoreLibs.Identity.Validators.Firebase;

public class FirebaseAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string PolicyName = "FirebaseService";

    public const string SchemeName = "Bearer";

    public string Issuer { get; set; } = null!;

    public string ConfigFilePath { get; set; } = null!;
}
