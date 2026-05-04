using System.Security.Claims;
using DenisCuciuc.Platform.Identity.Identity;

namespace DenisCuciuc.Platform.Identity.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.Claims.FirstOrDefault(x => x.Type == IdentityKeys.UserId);

        if (claim == null) throw new InvalidOperationException("Unauthorized context call");

        return long.TryParse(claim.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("Unauthorized context call");
    }
}
