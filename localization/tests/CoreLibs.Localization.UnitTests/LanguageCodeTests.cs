using CoreLibs.Localization;
namespace CoreLibs.Localization.UnitTests;

public sealed class LanguageCodeTests
{
    [Theory]
    [InlineData("RU", "ru")]
    [InlineData("En", "en")]
    public void Constructor_NormalizesToLower(string input, string expected)
    {
        var code = new LanguageCode(input);
        Assert.Equal(expected, code.Value);
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("eng")]
    [InlineData("r1")]
    public void Constructor_Throws_ForInvalidInput(string input)
    {
        Assert.Throws<ArgumentException>(() => new LanguageCode(input));
    }
    [Fact]
    public void FromCulture_ExtractsLanguagePart()
    {
        var code = LanguageCode.FromCulture("ru-RU");
        Assert.Equal("ru", code.Value);
    }
}
