using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Schema;
using CoreLibs.LiveConfig.GSheet.Validation;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class AttributeRowValidatorTests
{
    public class ItemRow
    {
        [GSheetColumn("Name")]
        [GSheetRequired]
        public string Name { get; set; } = "";

        [GSheetColumn("Level")]
        [GSheetRange(1, 100)]
        public int Level { get; set; }

        [GSheetColumn("Code")]
        [GSheetRegex(@"^[A-Z]{3}-\d{3}$")]
        public string Code { get; set; } = "";
    }

    [Fact]
    public void Validate_AllRowsValid_ReturnsOk()
    {
        var rows = new List<ItemRow>
        {
            new() { Name = "Sword", Level = 10, Code = "ABC-123" },
            new() { Name = "Shield", Level = 50, Code = "DEF-456" }
        };

        var result = AttributeRowValidator.Validate<ItemRow>(rows);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_RequiredFieldEmpty_ReturnsError()
    {
        var rows = new List<ItemRow>
        {
            new() { Name = "", Level = 10, Code = "ABC-123" }
        };

        var result = AttributeRowValidator.Validate<ItemRow>(rows);

        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Contains("required", result.Errors[0].Reason);
    }

    [Fact]
    public void Validate_ValueBelowRange_ReturnsError()
    {
        var rows = new List<ItemRow>
        {
            new() { Name = "Sword", Level = 0, Code = "ABC-123" }
        };

        var result = AttributeRowValidator.Validate<ItemRow>(rows);

        Assert.False(result.IsSuccess);
        Assert.Contains("outside range", result.Errors[0].Reason);
    }

    [Fact]
    public void Validate_ValueAboveRange_ReturnsError()
    {
        var rows = new List<ItemRow>
        {
            new() { Name = "Sword", Level = 999, Code = "ABC-123" }
        };

        var result = AttributeRowValidator.Validate<ItemRow>(rows);

        Assert.False(result.IsSuccess);
        Assert.Contains("outside range", result.Errors[0].Reason);
    }

    [Fact]
    public void Validate_RegexMismatch_ReturnsError()
    {
        var rows = new List<ItemRow>
        {
            new() { Name = "Sword", Level = 10, Code = "invalid" }
        };

        var result = AttributeRowValidator.Validate<ItemRow>(rows);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not match pattern", result.Errors[0].Reason);
    }

    [Fact]
    public void Validate_MultipleErrors_ReportsAll()
    {
        var rows = new List<ItemRow>
        {
            new() { Name = "", Level = 0, Code = "bad" }
        };

        var result = AttributeRowValidator.Validate<ItemRow>(rows);

        Assert.False(result.IsSuccess);
        // Required fails for Name (skips further checks), Range fails for Level, Regex fails for Code
        Assert.True(result.Errors.Count >= 2);
    }

    [Fact]
    public void Validate_EmptyRows_ReturnsOk()
    {
        var rows = new List<ItemRow>();

        var result = AttributeRowValidator.Validate<ItemRow>(rows);

        Assert.True(result.IsSuccess);
    }

    // --- Row type with no validation attributes ---
    public class PlainRow
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    [Fact]
    public void Validate_NoValidationAttributes_ReturnsOk()
    {
        var rows = new List<PlainRow> { new() { Name = "", Value = -5 } };

        var result = AttributeRowValidator.Validate<PlainRow>(rows);

        Assert.True(result.IsSuccess);
    }
}
