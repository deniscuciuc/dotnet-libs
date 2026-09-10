using CoreLibs.Identity.Base;
using CoreLibs.Identity.Token;

namespace CoreLibs.Identity.Identity;

public class IdentityDecoder(IList<ITokenValidator> tokenValidators) : IIdentityDecoder
{
    public IdentityClaims Decode(string scheme, string token)
    {
        if (!string.Equals(scheme, ServiceBaseAuthenticationOptions.SchemeName)) return IdentityClaims.None;

        foreach (var validator in tokenValidators)
        {
            var context = validator.CreateContext(token);

            if (context.Valid) return context.ServiceToken!.GetIdentity();
        }

        return IdentityClaims.None;
    }
}
