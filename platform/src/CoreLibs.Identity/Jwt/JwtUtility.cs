using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoreLibs.Identity.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CoreLibs.Identity.Jwt;

public class JwtUtility(IOptions<JwtOptions> options) : IJwtUtility
{
    private readonly JwtOptions _options = options.Value;

    public string Generate(IdentityClaims identityClaims)
    {
        var claims = identityClaims.Select(x => new Claim(x.Key, x.Value)).ToArray();

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var securityToken = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: DateTime.UtcNow.Add(_options.Lifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(securityToken);
    }

    public static bool TryParse(string token, out IdentityClaims claims)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();

            var securityToken = handler.ReadJwtToken(token);

            claims = new IdentityClaims(securityToken.Claims);

            return true;
        }
        catch
        {
            claims = IdentityClaims.None;

            return false;
        }
    }
}
