using Microsoft.AspNetCore.Authentication;

namespace CoreLibs.Identity.Base;

public abstract class ServiceBaseAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string PolicyName = "Service";

    public const string SchemeName = "Basic";

    public string Secret { get; set; } = null!;
}
