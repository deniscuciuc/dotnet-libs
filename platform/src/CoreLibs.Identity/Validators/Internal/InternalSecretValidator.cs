using CoreLibs.Identity.Token;

namespace CoreLibs.Identity.Validators.Internal;

public class InternalSecretValidator : TokenValidatorBase
{
    public const string Provider = "InternalSecret";

    private const string Prefix = "Secret";
    private const string Separator = "/";

    public override int Order => 30;

    public override TokenValidationContext CreateContext(string authorizationToken)
    {
        if (!authorizationToken.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return new TokenValidationContext(authorizationToken, null);

        try
        {
            var segments = authorizationToken.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length != 2) return new TokenValidationContext(authorizationToken, null);

            var secret = segments[1];

            var serviceToken = new ServiceToken
            {
                Uid = string.Empty,
                Email = string.Empty,
                Name = string.Empty,
                Secret = secret,
                LoginProvider = Provider
            };

            return new TokenValidationContext(authorizationToken, serviceToken);
        }
        catch
        {
            // ignored
        }

        return new TokenValidationContext(authorizationToken, null);
    }
}
