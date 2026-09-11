namespace CoreLibs.Identity.Token;

public interface IOrderedTokenValidator : ITokenValidator
{
    int Order { get; }
}
