using DenisCuciuc.Platform.Identity.Identity;

namespace DenisCuciuc.Platform.Identity.Tests;

public class IdentityClaimsTests
{
    [Fact]
    public void TryParse_ValidInput_ReturnsTrue()
    {
        var result = IdentityClaims.TryParse("key1=val1;key2=val2;", out var claims);

        Assert.True(result);
        Assert.Equal(2, claims.Count);
        Assert.Equal("val1", claims["key1"]);
        Assert.Equal("val2", claims["key2"]);
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsTrue_EmptyClaims()
    {
        var result = IdentityClaims.TryParse("", out var claims);

        Assert.True(result);
        Assert.Empty(claims.Keys);
    }

    [Fact]
    public void TryParse_InvalidSegment_ReturnsFalse()
    {
        var result = IdentityClaims.TryParse("invalid", out var claims);

        Assert.False(result);
        Assert.Equal(IdentityClaims.None, claims);
    }

    [Fact]
    public void TryParse_DuplicateKey_ReturnsFalse()
    {
        var result = IdentityClaims.TryParse("key=val1;key=val2;", out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_SegmentWithMultipleEquals_ReturnsFalse()
    {
        var result = IdentityClaims.TryParse("key=val=extra;", out _);

        Assert.False(result);
    }

    [Fact]
    public void Equality_SamePairs_AreEqual()
    {
        IdentityClaims.TryParse("a=1;b=2;", out var x);
        IdentityClaims.TryParse("a=1;b=2;", out var y);

        Assert.Equal(x, y);
        Assert.True(x == y);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        IdentityClaims.TryParse("a=1;", out var x);
        IdentityClaims.TryParse("a=2;", out var y);

        Assert.NotEqual(x, y);
        Assert.True(x != y);
    }

    [Fact]
    public void ContainsKey_ReturnsTrue_WhenPresent()
    {
        IdentityClaims.TryParse("key=val;", out var claims);

        Assert.True(claims.ContainsKey("key"));
        Assert.False(claims.ContainsKey("missing"));
    }

    [Fact]
    public void TryGetValue_ReturnsValue_WhenPresent()
    {
        IdentityClaims.TryParse("key=val;", out var claims);

        var found = claims.TryGetValue("key", out var value);

        Assert.True(found);
        Assert.Equal("val", value);
    }

    [Fact]
    public void ToString_ProducesRoundTrippableString()
    {
        IdentityClaims.TryParse("a=1;b=2;", out var original);

        var serialized = original.ToString();
        var result = IdentityClaims.TryParse(serialized, out var parsed);

        Assert.True(result);
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void None_IsEmpty()
    {
        Assert.Empty(IdentityClaims.None.Keys);
    }

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        IdentityClaims.TryParse("x=1;y=2;", out var claims);

        Assert.Contains("x", claims.Keys);
        Assert.Contains("y", claims.Keys);
    }
}
