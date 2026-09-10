using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Pipeline;

namespace CoreLibs.LiveConfig.UnitTests;

public class GSheetImporterAttributeTests
{
    [Fact]
    public void Needs_AllowsPipelineDependencies()
    {
        var exception = Record.Exception(() => new GSheetImporterAttribute("Items")
        {
            Needs = [typeof(TestPipelineDependency)]
        });

        Assert.Null(exception);
    }

    [Fact]
    public void RegistryAdd_RejectsOpenGenericEntityPipelineAdapter()
    {
        var registry = new GSheetImporterRegistry();

        Assert.Throws<InvalidOperationException>(() => registry.Add(typeof(EntityPipelineAdapter<,>)));
    }

    private sealed class TestPipelineDependency : IGSheetPipeline<TestRow, TestDomain>
    {
        public IEnumerable<TestDomain> MapToDomain(IReadOnlyList<TestRow> rows, GSheetImportContext context)
        {
            return [];
        }
    }

    private sealed record TestRow(string Id);

    private sealed record TestDomain(string Id);
}
