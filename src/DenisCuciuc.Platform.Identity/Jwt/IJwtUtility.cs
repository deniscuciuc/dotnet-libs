using DenisCuciuc.Platform.Identity.Identity;

namespace DenisCuciuc.Platform.Identity.Jwt;

public interface IJwtUtility
{
    string Generate(IdentityClaims identityClaims);
}
