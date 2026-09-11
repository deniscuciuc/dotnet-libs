using CoreLibs.Identity.Settings;
using CoreLibs.Identity.Token;
using Microsoft.Extensions.Options;

namespace CoreLibs.Identity.Validators.DebugValidator;

public class OpenCompositeTokenValidator(IOptionsMonitor<ServiceOptions> options) : TokenValidatorBase
{
    private const string Prefix = "Composite";
    private const string Separator = "/";
    private readonly ServiceOptions _options = options.CurrentValue;

    public override int Order => 20;

    public override TokenValidationContext CreateContext(string authorizationToken)
    {
        if (!_options.IsDevEnvironment ||
            !authorizationToken.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return new TokenValidationContext(authorizationToken, null);

        try
        {
            var segments = authorizationToken.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length != 4) return new TokenValidationContext(authorizationToken, null);

            var provider = segments[0];
            var secret = segments[1];
            var userId = segments[2];
            var email = segments[3];

            var serviceToken = new ServiceToken
            {
                Uid = userId,
                Email = email,
                Name = email,
                Secret = secret,
                LoginProvider = provider
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
