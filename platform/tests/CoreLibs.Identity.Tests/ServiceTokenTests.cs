using CoreLibs.Identity.Identity;
using CoreLibs.Identity.Token;

namespace CoreLibs.Identity.Tests;

public class ServiceTokenTests
{
    [Fact]
    public void JsonPropertyNames_AreDistinct()
    {
        var token = new ServiceToken
        {
            Uid = "u1",
            Name = "Test",
            Metadata = "meta",
            LoginProvider = "google",
            Email = "test@test.com",
            Registered = true,
            Secret = "secret",
            ExpireAt = DateTime.UtcNow.AddHours(1)
        };

        var json = System.Text.Json.JsonSerializer.Serialize(token);

        Assert.Contains("\"uid\"", json);
        Assert.Contains("\"name\"", json);
        Assert.Contains("\"m\"", json);
        Assert.Contains("\"lp\"", json);
        Assert.Contains("\"e\"", json);
        Assert.Contains("\"reg\"", json);
        Assert.Contains("\"s\"", json);
        Assert.Contains("\"exp\"", json);
    }

    [Fact]
    public void GetIdentity_MapsFieldsToClaims()
    {
        var token = new ServiceToken
        {
            Uid = "user-123",
            Name = "TestUser",
            Metadata = "{}",
            LoginProvider = "auth0",
            Email = "user@example.com",
            Registered = true,
            Secret = "s",
            ExpireAt = DateTime.UtcNow
        };

        var claims = token.GetIdentity();

        Assert.Equal("user-123", claims[IdentityKeys.UserId]);
        Assert.Equal("TestUser", claims[IdentityKeys.UserName]);
        Assert.Equal("auth0", claims[IdentityKeys.LoginProvider]);
        Assert.Equal("user@example.com", claims[IdentityKeys.Email]);
        Assert.Equal("{}", claims[IdentityKeys.Metadata]);
    }

    [Fact]
    public void TryParse_InvalidBase64_ReturnsFalse()
    {
        var result = ServiceToken.TryParse("not-valid-base64!!!", out var token);

        Assert.False(result);
        Assert.Null(token);
    }
}
