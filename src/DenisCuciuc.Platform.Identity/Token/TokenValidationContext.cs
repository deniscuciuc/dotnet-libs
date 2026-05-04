namespace DenisCuciuc.Platform.Identity.Token;

public class TokenValidationContext(string authorizationToken, ServiceToken? serviceToken)
{
    public string AuthorizationToken { get; } = authorizationToken;

    public ServiceToken? ServiceToken { get; } = serviceToken;

    public bool Valid => !string.IsNullOrWhiteSpace(AuthorizationToken) && ServiceToken != null;
}
