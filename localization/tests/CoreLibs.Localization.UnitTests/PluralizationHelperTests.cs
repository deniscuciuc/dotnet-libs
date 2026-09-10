using CoreLibs.Localization.Runtime;
namespace CoreLibs.Localization.UnitTests;

public sealed class PluralizationHelperTests
{
    private static Dictionary<string, string> Forms => new()
    {
        ["one"] = "1 предмет",
        ["few"] = "{count} предмета",
        ["many"] = "{count} предметов",
        ["other"] = "{count} предметов"
    };
    [Theory]
    [InlineData(1, "1 предмет")]
    [InlineData(2, "{count} предмета")]
    [InlineData(5, "{count} предметов")]
    [InlineData(11, "{count} предметов")]
    [InlineData(21, "1 предмет")]
    public void SelectForm_ReturnsCorrectCategory(long count, string expected)
    {
        var result = PluralizationHelper.SelectForm(Forms, count);
        Assert.Equal(expected, result);
    }
}
