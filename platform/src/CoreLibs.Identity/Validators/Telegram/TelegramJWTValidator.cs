using System.IdentityModel.Tokens.Jwt;
using CoreLibs.Identity.Extensions;
using CoreLibs.Identity.Jwt;
using CoreLibs.Identity.Settings;
using CoreLibs.Identity.Token;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CoreLibs.Identity.Validators.Telegram;

public class TelegramJWTValidator(IOptionsMonitor<TelegramAuthOptions> options)
    : TokenValidatorBase, ITelegramJWTValidator
{
    public override int Order => 16;


    public override TokenValidationContext CreateContext(string authorizationToken)
    {
        if (!JwtUtility.TryParse(authorizationToken, out var claims))
            return new TokenValidationContext(authorizationToken, null);

        try
        {
            var audience = claims[JwtKeys.Audience];

            if (audience != options.CurrentValue.Audience)
                return new TokenValidationContext(authorizationToken, null);

            if (claims[JwtKeys.Issuer] != options.CurrentValue.Issuer)
                throw new Exception("The issuer for the Telegram provider is wrong");

            var serviceToken = new ServiceToken
            {
                Uid = TelegramUserId.GeExternalUserIdByTelegramUserId(claims[JwtKeys.UserId]),
                Email = string.Empty,
                Name = claims[JwtKeys.UserName],
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

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateLifetime = true,
            ValidateAudience = false,
            ValidateIssuer = false,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(options.CurrentValue.JwtSecretBase64))
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
