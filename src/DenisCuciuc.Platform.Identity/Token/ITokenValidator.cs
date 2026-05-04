namespace DenisCuciuc.Platform.Identity.Token;

public interface ITokenValidator
{
    TokenValidationContext CreateContext(string authorizationToken);

    Task<bool> ValidateAsync(TokenValidationContext validationContext);
}
