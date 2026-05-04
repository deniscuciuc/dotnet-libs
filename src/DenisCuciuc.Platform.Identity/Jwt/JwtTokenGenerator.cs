using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace DenisCuciuc.Platform.Identity.Jwt;

public static class JwtTokenGenerator
{
    public static string GenerateJwtToken(string userId, string userName, string userEmail, string secretKey,
        string issuer, string audience, int expireMinutes = 60, string? userMetadata = null)
    {
        var securityKey = new SymmetricSecurityKey(Convert.FromBase64String(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new(JwtKeys.UserId, userId),
            new(JwtKeys.UserName, userName),
            new(JwtKeys.UserEmail, userEmail),
            new(JwtKeys.UserMetadata, userMetadata ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }
}
