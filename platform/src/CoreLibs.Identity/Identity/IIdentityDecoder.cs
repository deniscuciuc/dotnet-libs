namespace CoreLibs.Identity.Identity;

public interface IIdentityDecoder
{
    IdentityClaims Decode(string scheme, string token);
}
