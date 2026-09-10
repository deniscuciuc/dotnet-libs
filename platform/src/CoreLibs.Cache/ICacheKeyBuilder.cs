namespace CoreLibs.Cache;

public interface ICacheKeyBuilder
{
    string Build(params object[] parts);
}
