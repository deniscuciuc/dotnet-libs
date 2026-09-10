using CoreLibs.Identity.Identity;
using Microsoft.AspNetCore.Http;

namespace CoreLibs.Identity.Extensions;

public static class IdentityClaimsExtensions
{
    public static IdentityClaims GetIdentity(this HttpContext context)
    {
        if (!context.Items.TryGetValue(nameof(IdentityClaims), out var identity)) identity = IdentityClaims.None;

        return (IdentityClaims)identity!;
    }

    public static void SetIdentity(this HttpContext context, IdentityClaims claims)
    {
        context.Items[nameof(IdentityClaims)] = claims;
    }

    public static string? GetIdentityClaim(this HttpContext context, string type)
    {
        return context.User.Claims.FirstOrDefault(x => x.Type == type)?.Value;
    }

    public static string GetIdentityValue(this HttpContext context, string key)
    {
        context.GetIdentity().TryGetValue(key, out var value);
        return value;
    }
}
