using CoreLibs.Identity.Identity;

namespace CoreLibs.Identity.Jwt;

public interface IJwtUtility
{
    string Generate(IdentityClaims identityClaims);
}
