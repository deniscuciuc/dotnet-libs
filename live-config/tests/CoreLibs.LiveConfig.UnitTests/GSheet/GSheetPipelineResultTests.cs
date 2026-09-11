using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class GSheetPipelineResultTests
{
    [Fact]
    public void Success_CreatesValidResult()
    {
        var data = new List<string> { "A", "B", "C" };

        var result = GSheetPipelineResult<string>.Success(data, 5, 3);

        Assert.Equal(3, result.Data.Count);
        Assert.Equal(5, result.TotalRows);
        Assert.Equal(3, result.ValidRows);
        Assert.Equal(2, result.SkippedRows);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void WithErrors_CreatesResultWithErrors()
    {
        var errors = new List<GSheetValidationError>
        {
            new(0, "Name", "required"),
            new(1, "Level", "out of range")
        };

        var result = GSheetPipelineResult<string>.WithErrors([], errors, 5, 3);

        Assert.Empty(result.Data);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal(5, result.TotalRows);
    }

    [Fact]
    public void Success_WithWarnings()
    {
        var data = new List<string> { "A" };
        var warnings = new List<GSheetWarning> { new("Extra", "Extra column found") };

        var result = GSheetPipelineResult<string>.Success(data, 1, 1, warnings);

        Assert.Single(result.Data);
        Assert.Single(result.Warnings);
    }
}
