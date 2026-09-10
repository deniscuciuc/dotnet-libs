using CoreLibs.LiveConfig.GSheet;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class GSheetValidationErrorEnhancedTests
{
    [Fact]
    public void ForSheet_ComputesSpreadsheetRow()
    {
        var error = GSheetValidationError.ForSheet("Items", 0, "Name", "is empty");

        Assert.Equal("Items", error.SheetName);
        Assert.Equal(2, error.SpreadsheetRow); // rowIndex 0 + 2 (header offset)
        Assert.Equal(0, error.RowIndex);
        Assert.Equal("Name", error.Field);
        Assert.Equal("is empty", error.Reason);
    }

    [Fact]
    public void ForSheet_Row5_ComputesSpreadsheetRow7()
    {
        var error = GSheetValidationError.ForSheet("Data", 5, "Code", "invalid");

        Assert.Equal(7, error.SpreadsheetRow);
    }

    [Fact]
    public void ToString_WithSheetAndRow_IncludesLocation()
    {
        var error = GSheetValidationError.ForSheet("Currencies", 3, "Rate", "out of range");

        var str = error.ToString();

        Assert.Contains("Currencies", str);
        Assert.Contains("Row 5", str);
        Assert.Contains("Rate", str);
        Assert.Contains("out of range", str);
    }

    [Fact]
    public void ToString_WithoutSheet_OmitsLocation()
    {
        var error = new GSheetValidationError(0, "Field", "bad");

        var str = error.ToString();

        Assert.Contains("Field", str);
        Assert.Contains("bad", str);
    }

    [Fact]
    public void BackwardCompatible_ThreeArgConstructor()
    {
        var error = new GSheetValidationError(1, "Col", "msg");

        Assert.Equal(1, error.RowIndex);
        Assert.Equal("Col", error.Field);
        Assert.Equal("msg", error.Reason);
        Assert.Null(error.SheetName);
        Assert.Null(error.SpreadsheetRow);
        Assert.Null(error.Column);
    }

    [Fact]
    public void FullConstructor_SetsAllFields()
    {
        var error = new GSheetValidationError(2, "F", "R",
            "Sheet1", 4, "B");

        Assert.Equal("Sheet1", error.SheetName);
        Assert.Equal(4, error.SpreadsheetRow);
        Assert.Equal("B", error.Column);
    }
}
