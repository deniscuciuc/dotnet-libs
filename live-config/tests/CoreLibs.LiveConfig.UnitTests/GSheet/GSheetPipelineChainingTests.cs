using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class GSheetPipelineChainingTests
{
    [Fact]
    public void UseValidator_CalledOnce_UsesDirectValidator()
    {
        var pipeline = new SingleValidatorPipeline();

        var validator = pipeline.RowValidator;

        Assert.NotNull(validator);
        Assert.IsType<AlwaysOkValidator>(validator);
    }

    [Fact]
    public void UseValidator_CalledTwice_CreatesComposite()
    {
        var pipeline = new DualValidatorPipeline();

        var validator = pipeline.RowValidator;

        Assert.NotNull(validator);
        Assert.IsType<CompositeRowValidator<TestRow>>(validator);
    }

    [Fact]
    public void UseValidator_CompositeChains_MergesErrors()
    {
        var pipeline = new DualValidatorPipeline();
        var validator = pipeline.RowValidator!;

        var result = validator.Validate([new TestRow("a")], new GSheetImportContext());

        // AlwaysOkValidator returns ok, FailingValidator returns 1 error → merged = 1 error
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void UseGraphValidator_CalledTwice_CreatesComposite()
    {
        var pipeline = new DualGraphValidatorPipeline();

        var validator = pipeline.GraphValidator;

        Assert.NotNull(validator);
        Assert.IsType<CompositeGraphValidator<TestRow>>(validator);
    }

    // ── Test pipelines ──

    [GSheetImporter("Test", "A1:A")]
    private sealed class SingleValidatorPipeline : GSheetPipeline<TestRow, string>
    {
        protected override void Configure()
        {
            UseValidator<AlwaysOkValidator>();
        }
    }

    [GSheetImporter("Test", "A1:A")]
    private sealed class DualValidatorPipeline : GSheetPipeline<TestRow, string>
    {
        protected override void Configure()
        {
            UseValidator<AlwaysOkValidator>();
            UseValidator<FailingValidator>();
        }
    }

    [GSheetImporter("Test", "A1:A")]
    private sealed class DualGraphValidatorPipeline : GSheetPipeline<TestRow, string>
    {
        protected override void Configure()
        {
            UseGraphValidator<OkGraphValidator>();
            UseGraphValidator<FailingGraphValidator>();
        }
    }

    public record TestRow([property: GSheetColumn("Name")] string Name);

    private sealed class AlwaysOkValidator : IRowValidator<TestRow>
    {
        public GSheetValidationResult Validate(IReadOnlyList<TestRow> rows, GSheetImportContext context)
        {
            return GSheetValidationResult.Ok();
        }
    }

    private sealed class FailingValidator : IRowValidator<TestRow>
    {
        public GSheetValidationResult Validate(IReadOnlyList<TestRow> rows, GSheetImportContext context)
        {
            return GSheetValidationResult.Fail(new GSheetValidationError(0, "Name", "always fails"));
        }
    }

    private sealed class OkGraphValidator : IGraphValidator<TestRow>
    {
        public GSheetValidationResult Validate(IReadOnlyList<TestRow> rows, GSheetImportContext context)
        {
            return GSheetValidationResult.Ok();
        }
    }

    private sealed class FailingGraphValidator : IGraphValidator<TestRow>
    {
        public GSheetValidationResult Validate(IReadOnlyList<TestRow> rows, GSheetImportContext context)
        {
            return GSheetValidationResult.Fail(new GSheetValidationError(0, "Name", "graph fail"));
        }
    }
}
