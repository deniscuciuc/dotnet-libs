using System.Diagnostics;
using CoreLibs.Identity.Jwt;
using CoreLibs.Identity.Token;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.Identity.Validators.Firebase;

public class FirebaseValidator(
    IOptionsMonitor<FirebaseAuthenticationOptions> optionsMonitor,
    ILogger<FirebaseAuthenticationHandler> logger) : TokenValidatorBase
{
    private static readonly TimeSpan WarningThreshold = TimeSpan.FromSeconds(3);

    private readonly FirebaseAuthenticationOptions _options =
        optionsMonitor.Get(FirebaseAuthenticationOptions.SchemeName);

    public override int Order => 40;

    public override TokenValidationContext CreateContext(string authorizationToken)
    {
        if (!JwtUtility.TryParse(authorizationToken, out var claims))
            return new TokenValidationContext(authorizationToken, null);

        try
        {
            if (!claims.TryGetValue(JwtKeys.UserName, out var userName)) userName = "Anonymous";

            var serviceToken = new ServiceToken
            {
                Uid = claims[JwtKeys.UserId],
                Name = userName
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

        FirebaseToken decodedToken;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            decodedToken =
                await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(validationContext.AuthorizationToken);
            stopwatch.Stop();
        }
        catch (FirebaseAuthException exception)
        {
            logger.LogWarning(
                "Firebase token verification - {ResponseCode}, {AuthErrorCode}",
                exception.ErrorCode,
                exception.AuthErrorCode);
            return false;
        }
        catch
        {
            logger.LogWarning("Firebase token verification 500");
            return false;
        }

        if (decodedToken.Issuer != _options.Issuer)
        {
            logger.LogWarning("Firebase token verification WrongIssuer {Duration}ms", stopwatch.ElapsedMilliseconds);
            return false;
        }

        if (stopwatch.Elapsed >= WarningThreshold)
            logger.LogWarning("Firebase token verification 200 {Duration}ms", stopwatch.ElapsedMilliseconds);

        if (decodedToken.Claims.TryGetValue("provider_id", out var loginProvider))
            validationContext.ServiceToken!.LoginProvider = loginProvider.ToString();

        validationContext.ServiceToken!.Registered = true;

        return true;
    }
}
