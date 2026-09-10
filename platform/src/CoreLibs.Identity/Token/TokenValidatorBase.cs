namespace CoreLibs.Identity.Token;

public abstract class TokenValidatorBase : IOrderedTokenValidator
{
    public abstract int Order { get; }

    public abstract TokenValidationContext CreateContext(string authorizationToken);

    public virtual Task<bool> ValidateAsync(TokenValidationContext validationContext)
    {
        return Task.FromResult(validationContext.Valid);
    }
}
