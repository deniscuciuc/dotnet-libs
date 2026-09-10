using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class ColumnNameNormalizerTests
{
    [Theory]
    [InlineData("CoinsAmount", "coinsamount")]
    [InlineData("coinsAmount", "coinsamount")]
    [InlineData("coinsamount", "coinsamount")]
    [InlineData("coins_amount", "coinsamount")]
    [InlineData("coins-amount", "coinsamount")]
    [InlineData("Coins Amount", "coinsamount")]
    [InlineData("COINS_AMOUNT", "coinsamount")]
    [InlineData("coins__amount", "coinsamount")]
    [InlineData("_coins_amount_", "coinsamount")]
    public void Normalize_AllConventions_ProduceSameResult(string input, string expected)
    {
        Assert.Equal(expected, ColumnNameNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Normalize_NullOrWhitespace_ReturnsEmpty(string? input)
    {
        Assert.Equal(string.Empty, ColumnNameNormalizer.Normalize(input!));
    }

    [Fact]
    public void Normalize_SingleWord_Lowercases()
    {
        Assert.Equal("name", ColumnNameNormalizer.Normalize("Name"));
        Assert.Equal("name", ColumnNameNormalizer.Normalize("NAME"));
        Assert.Equal("name", ColumnNameNormalizer.Normalize("name"));
    }
}
