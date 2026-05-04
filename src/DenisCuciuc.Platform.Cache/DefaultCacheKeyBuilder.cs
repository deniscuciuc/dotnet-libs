namespace DenisCuciuc.Platform.Cache;

public sealed class DefaultCacheKeyBuilder(string separator = ":") : ICacheKeyBuilder
{
    public string Build(params object[] parts) =>
        string.Join(separator, parts);
}
