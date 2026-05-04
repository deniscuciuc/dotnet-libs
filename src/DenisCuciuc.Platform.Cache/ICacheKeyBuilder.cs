namespace DenisCuciuc.Platform.Cache;

public interface ICacheKeyBuilder
{
    string Build(params object[] parts);
}
