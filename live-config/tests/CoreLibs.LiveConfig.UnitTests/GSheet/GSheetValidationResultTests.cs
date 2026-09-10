using CoreLibs.LiveConfig.GSheet;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class GSheetValidationResultTests
{
    [Fact]
    public void Ok_ReturnsValid()
    {
        var result = GSheetValidationResult.Ok();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Fail_ReturnsSingleError()
    {
        var result = GSheetValidationResult.Fail(new GSheetValidationError(0, "Field1", "something is wrong"));

        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("Field1", result.Errors[0].Field);
    }

    [Fact]
    public void Fail_WithErrorList()
    {
        var errors = new List<GSheetValidationError>
        {
            new(0, "A", "err1"),
            new(1, "B", "err2")
        };

        var result = GSheetValidationResult.Fail(errors);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Merge_CombinesResults()
    {
        var r1 = GSheetValidationResult.Fail(new GSheetValidationError(0, "A", "error in A"));
        var r2 = GSheetValidationResult.Fail(new GSheetValidationError(1, "B", "error in B"));

        var merged = GSheetValidationResult.Merge(r1, r2);

        Assert.False(merged.IsSuccess);
        Assert.Equal(2, merged.Errors.Count);
    }

    [Fact]
    public void Merge_AllOk_ReturnsOk()
    {
        var r1 = GSheetValidationResult.Ok();
        var r2 = GSheetValidationResult.Ok();

        var merged = GSheetValidationResult.Merge(r1, r2);

        Assert.True(merged.IsSuccess);
    }

    [Fact]
    public void Merge_MixedResults()
    {
        var ok = GSheetValidationResult.Ok();
        var fail = GSheetValidationResult.Fail(new GSheetValidationError(0, "X", "bad"));

        var merged = GSheetValidationResult.Merge(ok, fail);

        Assert.False(merged.IsSuccess);
        Assert.Single(merged.Errors);
    }

    [Fact]
    public void Error_ToString_WithRowIndex()
    {
        var error = new GSheetValidationError(5, "Name", "is required");
        var str = error.ToString();

        // RowIndex 5 → "Row 7" (RowIndex + 2 for header offset)
        Assert.Contains("Row 7", str);
        Assert.Contains("Name", str);
    }

    [Fact]
    public void Error_ToString_StructuralError()
    {
        var error = new GSheetValidationError(-1, "Header", "missing column");
        var str = error.ToString();

        Assert.Contains("Header", str);
        Assert.DoesNotContain("Row", str);
    }
}
