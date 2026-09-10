using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Pipeline;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class CompositeValidatorTests
{
    // ── CompositeRowValidator ──

    [Fact]
    public void CompositeRowValidator_NoValidators_ReturnsOk()
    {
        var composite = new CompositeRowValidator<string>();

        var result = composite.Validate(["a", "b"], new GSheetImportContext());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CompositeRowValidator_SingleValidator_DelegatesToIt()
    {
        var composite = new CompositeRowValidator<int>();
        composite.Add(new FailingRowValidator(1, "Field", "bad"));

        var result = composite.Validate([10], new GSheetImportContext());

        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("bad", result.Errors[0].Reason);
    }

    [Fact]
    public void CompositeRowValidator_MultipleValidators_MergesErrors()
    {
        var composite = new CompositeRowValidator<int>();
        composite.Add(new FailingRowValidator(0, "A", "err1"));
        composite.Add(new FailingRowValidator(1, "B", "err2"));

        var result = composite.Validate([10, 20], new GSheetImportContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void CompositeRowValidator_MixedResults_MergesCorrectly()
    {
        var composite = new CompositeRowValidator<int>();
        composite.Add(new OkRowValidator());
        composite.Add(new FailingRowValidator(0, "F", "oops"));

        var result = composite.Validate([1], new GSheetImportContext());

        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
    }

    // ── CompositeGraphValidator ──

    [Fact]
    public void CompositeGraphValidator_NoValidators_ReturnsOk()
    {
        var composite = new CompositeGraphValidator<string>();

        var result = composite.Validate(["x"], new GSheetImportContext());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CompositeGraphValidator_MultipleValidators_MergesErrors()
    {
        var composite = new CompositeGraphValidator<string>();
        composite.Add(new FailingGraphValidator(0, "G1", "graph err1"));
        composite.Add(new FailingGraphValidator(1, "G2", "graph err2"));

        var result = composite.Validate(["a", "b"], new GSheetImportContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void CompositeGraphValidator_SingleOk_ReturnsOk()
    {
        var composite = new CompositeGraphValidator<string>();
        composite.Add(new OkGraphValidator());

        var result = composite.Validate(["a"], new GSheetImportContext());

        Assert.True(result.IsSuccess);
    }

    // ── Helpers ──

    private sealed class OkRowValidator : IRowValidator<int>
    {
        public GSheetValidationResult Validate(IReadOnlyList<int> rows, GSheetImportContext context)
        {
            return GSheetValidationResult.Ok();
        }
    }

    private sealed class FailingRowValidator(int row, string field, string reason) : IRowValidator<int>
    {
        public GSheetValidationResult Validate(IReadOnlyList<int> rows, GSheetImportContext context)
        {
            return GSheetValidationResult.Fail(new GSheetValidationError(row, field, reason));
        }
    }

    private sealed class OkGraphValidator : IGraphValidator<string>
    {
        public GSheetValidationResult Validate(IReadOnlyList<string> rows, GSheetImportContext context)
        {
            return GSheetValidationResult.Ok();
        }
    }

    private sealed class FailingGraphValidator(int row, string field, string reason) : IGraphValidator<string>
    {
        public GSheetValidationResult Validate(IReadOnlyList<string> rows, GSheetImportContext context)
        {
            return GSheetValidationResult.Fail(new GSheetValidationError(row, field, reason));
        }
    }
}
