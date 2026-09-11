using CoreLibs.Localization.Runtime;
namespace CoreLibs.Localization.UnitTests;

public sealed class InterpolationHelperTests
{
    [Theory]
    [InlineData("Hello {name}!", new[] { "name", "World" }, "Hello World!")]
    [InlineData("No placeholders", new string[0], "No placeholders")]
    [InlineData("{a} and {b}", new[] { "a", "X", "b", "Y" }, "X and Y")]
    public void Interpolate_ReplacesNamedPlaceholders(string template, string[] kvPairs, string expected)
    {
        var dict = new Dictionary<string, string>();
        for (var i = 0; i < kvPairs.Length; i += 2)
            dict[kvPairs[i]] = kvPairs[i + 1];
        var result = InterpolationHelper.Interpolate(template, dict);
        Assert.Equal(expected, result);
    }
    [Fact]
    public void Interpolate_WithAnonymousObject_Works()
    {
        var result = InterpolationHelper.Interpolate("Hi {user}", LocalizationArgs.With("user", "Alice"));
        Assert.Equal("Hi Alice", result);
    }
    [Fact]
    public void Interpolate_NullArgs_ReturnsTemplate()
    {
        var result = InterpolationHelper.Interpolate("Hello {name}", null);
        Assert.Equal("Hello {name}", result);
    }
}
