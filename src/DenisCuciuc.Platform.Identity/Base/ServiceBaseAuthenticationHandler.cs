using System.Security.Claims;
using System.Text.Encodings.Web;
using DenisCuciuc.Platform.Identity.Extensions;
using DenisCuciuc.Platform.Identity.Identity;
using DenisCuciuc.Platform.Identity.Token;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace DenisCuciuc.Platform.Identity.Base;

public class ServiceBaseAuthenticationHandler<T>(
    IOptionsMonitor<T> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TimeProvider timeProvider,
    IList<ITokenValidator> tokenValidators,
    IMemoryCache cache
) : AuthenticationHandler<T>(options, logger, encoder)
    where T : ServiceBaseAuthenticationOptions, new()
{
    private static TimeSpan TokenCacheExpiration => TimeSpan.FromSeconds(15);

    protected override Task InitializeHandlerAsync()
    {
        Options.TimeProvider = timeProvider;
        return base.InitializeHandlerAsync();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!TryGetAuthorizationToken(out var authorizationToken)) return AuthenticateResult.NoResult();

        var (serviceToken, lastErrorMessage) = await TryGetServiceTokenAsync(authorizationToken);

        if (serviceToken is null) return AuthenticateResult.Fail(lastErrorMessage ?? "Invalid token");

        if (!IsTokenValid(serviceToken))
            return AuthenticateResult.Fail(lastErrorMessage ?? "Invalid secret");

        Context.SetIdentity(serviceToken.GetIdentity());

        var ticket = GetAuthenticationTicket(serviceToken);

        return AuthenticateResult.Success(ticket);
    }

    protected virtual bool IsTokenValid(ServiceToken serviceToken)
    {
        return serviceToken.Secret == Options.Secret;
    }

    private bool TryGetAuthorizationToken(out string authorizationToken)
    {
        authorizationToken = null!;

        if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorizationHeader)) return false;

        var authorizationHeaderString = authorizationHeader.ToString();

        if (!authorizationHeaderString.StartsWith(Scheme.Name, StringComparison.OrdinalIgnoreCase)) return false;

        if (authorizationHeaderString.Length <= Scheme.Name.Length + 1) return false;

        authorizationToken = authorizationHeaderString[(Scheme.Name.Length + 1)..];

        return true;
    }

    private async Task<(ServiceToken?, string?)> TryGetServiceTokenAsync(string authorizationToken)
    {
        if (cache.TryGetValue(authorizationToken, out ServiceToken? serviceToken)) return (serviceToken, null);

        string? lastErrorMessage = null;

        foreach (var validator in tokenValidators)
        {
            var context = validator.CreateContext(authorizationToken);

            if (!context.Valid)
            {
                lastErrorMessage = "Service token bad format";
            }
            else if (!await validator.ValidateAsync(context))
            {
                lastErrorMessage = "Service token validation failed";
            }
            else
            {
                cache.Set(authorizationToken, context.ServiceToken, TokenCacheExpiration);

                return (context.ServiceToken, null);
            }
        }

        return (null, lastErrorMessage);
    }

    private AuthenticationTicket GetAuthenticationTicket(ServiceToken serviceToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, serviceToken.Uid),
            new(IdentityKeys.UserId, serviceToken.Uid),
            new(IdentityKeys.Email, serviceToken.Email),
            new(IdentityKeys.UserName, serviceToken.Name),
            new(IdentityKeys.Metadata, serviceToken.Metadata ?? string.Empty),
            new(IdentityKeys.LoginProvider, serviceToken.LoginProvider!)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return ticket;
    }
}
