using System.IdentityModel.Tokens.Jwt;
using DenisCuciuc.Platform.Identity.Extensions;
using DenisCuciuc.Platform.Identity.Jwt;
using DenisCuciuc.Platform.Identity.Settings;
using DenisCuciuc.Platform.Identity.Token;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DenisCuciuc.Platform.Identity.Validators.Production;

public class OrganizationApiKeyJwtTokenValidator(
    IOptionsMonitor<ServiceOptions> serviceOptions,
    IOptionsMonitor<ApiTokenAuthOptions> authOptions)
    : TokenValidatorBase
{
    public override int Order => 15;


    public override TokenValidationContext CreateContext(string authorizationToken)
    {
        if (!JwtUtility.TryParse(authorizationToken, out var claims))
            return new TokenValidationContext(authorizationToken, null);

        try
        {
            var audience = claims[JwtKeys.Audience];

            // TODO: Use audience from settings
            if (audience != $"Service.Api.{serviceOptions.CurrentValue.EnvironmentName}")
                return new TokenValidationContext(authorizationToken, null);

            var serviceToken = new ServiceToken
            {
                Uid = claims[JwtKeys.UserId],
                Email = claims[JwtKeys.UserEmail],
                Name = claims[JwtKeys.UserName],
                Metadata = claims[JwtKeys.UserMetadata],
                LoginProvider = claims[JwtKeys.Issuer],
                ExpireAt = Convert.ToInt64(claims["exp"]).UnixToDateTime()
            };

            return new TokenValidationContext(authorizationToken, serviceToken);
        }
        catch
        {
            // ignored  
        }

        return new TokenValidationContext(authorizationToken, null);
    }

    public override async Task<bool> ValidateAsync(TokenValidationContext validationContext)
    {
        if (!await base.ValidateAsync(validationContext)) return false;

        if (validationContext.ServiceToken!.ExpireAt < DateTime.UtcNow) return false;

        if (!authOptions.CurrentValue.ActiveKeys.TryGetValue(
                validationContext.ServiceToken.LoginProvider!,
                out var secret))
            return false;

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateLifetime = true,
            ValidateAudience = false,
            ValidateIssuer = false,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(secret))
        };

        try
        {
            handler.ValidateToken(validationContext.AuthorizationToken, parameters, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
