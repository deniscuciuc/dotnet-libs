using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Schema;
using CoreLibs.LiveConfig.GSheet.Validation;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class StructuralValidatorTests
{
    public class TestRow
    {
        [GSheetColumn("Name")] public string Name { get; set; } = "";

        [GSheetColumn("Level")] public int Level { get; set; }

        [GSheetColumn("Active")] public bool Active { get; set; }
    }

    [Fact]
    public void Validate_AllColumnsPresent_ReturnsOk()
    {
        var headers = new List<string> { "Name", "Level", "Active" };

        var result = StructuralValidator.Validate<TestRow>(headers, out var warnings);

        Assert.True(result.IsSuccess);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Validate_MissingColumn_ReturnsError()
    {
        var headers = new List<string> { "Name", "Active" };

        var result = StructuralValidator.Validate<TestRow>(headers, out _);

        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Contains("Level", result.Errors[0].Field);
    }

    [Fact]
    public void Validate_ExtraColumn_ReturnsWarning()
    {
        var headers = new List<string> { "Name", "Level", "Active", "Bonus" };

        var result = StructuralValidator.Validate<TestRow>(headers, out var warnings);

        Assert.True(result.IsSuccess);
        Assert.Single(warnings);
        Assert.Contains("Bonus", warnings[0].Field);
    }

    [Fact]
    public void Validate_CaseInsensitive_PassesForMatchingHeaders()
    {
        var headers = new List<string> { "name", "level", "active" };

        var result = StructuralValidator.Validate<TestRow>(headers, out var warnings);

        Assert.True(result.IsSuccess);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Validate_MultipleColumnsMissing_ReportsAll()
    {
        var headers = new List<string> { "Active" };

        var result = StructuralValidator.Validate<TestRow>(headers, out _);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Validate_EmptyHeaders_ReportsAllMissing()
    {
        var headers = new List<string>();

        var result = StructuralValidator.Validate<TestRow>(headers, out _);

        Assert.False(result.IsSuccess);
        Assert.Equal(3, result.Errors.Count);
    }

    // --- Naming convention tests ---

    public class NamingConventionRow
    {
        [GSheetColumn("CoinsAmount")] public decimal CoinsAmount { get; set; }

        [GSheetColumn("PlayerName")] public string PlayerName { get; set; } = "";
    }

    [Theory]
    [InlineData("coins_amount", "player_name")] // snake_case
    [InlineData("coinsAmount", "playerName")] // camelCase
    [InlineData("CoinsAmount", "PlayerName")] // PascalCase
    [InlineData("coinsamount", "playername")] // flatcase
    [InlineData("COINS_AMOUNT", "PLAYER_NAME")] // UPPER_SNAKE
    [InlineData("coins-amount", "player-name")] // kebab-case
    [InlineData("Coins Amount", "Player Name")] // Title Case with spaces
    public void Validate_NamingConventions_AllMatch(string header1, string header2)
    {
        var headers = new List<string> { header1, header2 };

        var result = StructuralValidator.Validate<NamingConventionRow>(headers, out var warnings);

        Assert.True(result.IsSuccess);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Validate_SnakeCaseHeaders_NoExtraColumnWarning()
    {
        // Property is "CoinsAmount" but sheet header is "coins_amount" — should match, not warn
        var headers = new List<string> { "coins_amount", "player_name" };

        var result = StructuralValidator.Validate<NamingConventionRow>(headers, out var warnings);

        Assert.True(result.IsSuccess);
        Assert.Empty(warnings);
    }
}
