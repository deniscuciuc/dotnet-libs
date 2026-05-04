namespace DenisCuciuc.Platform.Identity.Token;

public interface IOrderedTokenValidator : ITokenValidator
{
    int Order { get; }
}
