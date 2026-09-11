namespace CoreLibs.Cache.Tests;

public class DefaultCacheKeyBuilderTests
{
    [Fact]
    public void Build_JoinsParts_WithDefaultSeparator()
    {
        var builder = new DefaultCacheKeyBuilder();

        var key = builder.Build("prefix", "entity", 123);

        Assert.Equal("prefix:entity:123", key);
    }

    [Fact]
    public void Build_UsesCustomSeparator()
    {
        var builder = new DefaultCacheKeyBuilder("-");

        var key = builder.Build("a", "b", "c");

        Assert.Equal("a-b-c", key);
    }

    [Fact]
    public void Build_SinglePart_ReturnsWithoutSeparator()
    {
        var builder = new DefaultCacheKeyBuilder();

        var key = builder.Build("solo");

        Assert.Equal("solo", key);
    }
}
