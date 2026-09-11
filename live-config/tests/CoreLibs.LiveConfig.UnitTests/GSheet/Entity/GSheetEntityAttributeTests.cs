using CoreLibs.LiveConfig.GSheet.Entity;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Entity;

public class GSheetEntityAttributeTests
{
    [Fact]
    public void Constructor_ValidSheetAndRange_SetsProperties()
    {
        var attr = new GSheetEntityAttribute("Currencies", "A1:C");

        Assert.Equal("Currencies", attr.SheetName);
        Assert.Equal("A1:C", attr.Range);
    }

    [Fact]
    public void Constructor_NoRange_DefaultsToEmpty()
    {
        var attr = new GSheetEntityAttribute("Currencies");

        Assert.Equal("Currencies", attr.SheetName);
        Assert.Equal("", attr.Range);
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var attr = new GSheetEntityAttribute("  Sheet1  ", "  B2:G  ");

        Assert.Equal("Sheet1", attr.SheetName);
        Assert.Equal("B2:G", attr.Range);
    }

    [Fact]
    public void Constructor_NullSheetName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new GSheetEntityAttribute(null!));
    }

    [Fact]
    public void Constructor_EmptySheetName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new GSheetEntityAttribute(""));
    }

    [Fact]
    public void Constructor_InvalidRange_Throws()
    {
        Assert.Throws<ArgumentException>(() => new GSheetEntityAttribute("Sheet", "invalid!!!"));
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("A:Z")]
    [InlineData("A1:Z")]
    [InlineData("A:Z50")]
    [InlineData("A1:C100")]
    [InlineData("AA1:ZZ100")]
    public void Constructor_ValidRangeFormats_Accepted(string range)
    {
        var attr = new GSheetEntityAttribute("Sheet", range);
        Assert.Equal(range, attr.Range);
    }
}
