using CoreLibs.LiveConfig.GSheet;

namespace CoreLibs.LiveConfig.UnitTests;

public class GSheetImporterRegistryTests
{
    [Fact]
    public void ResolveImporterType_AllowsHyphenatedConfigNames()
    {
        var resolved = GSheetImporterRegistry.ResolveImporterType("hyphenated-config");

        Assert.Equal(typeof(HyphenatedConfigImporter), resolved);
    }

    [Fact]
    public void DiscoverImporterTypes_IgnoresPipelineTypesWithoutImporterAttribute()
    {
        var discovered = GSheetImporterRegistry.DiscoverImporterTypes().ToList();

        Assert.DoesNotContain(typeof(PipelineWithoutAttribute), discovered);
    }

    [GSheetImporter("HyphenatedConfig")]
    private sealed class HyphenatedConfigImporter : IGSheetImporter<HyphenatedRow, HyphenatedDomain>
    {
        public CsvHelper.Configuration.ClassMap<HyphenatedRow> CreateMapper()
        {
            throw new NotSupportedException();
        }

        public GSheetValidationResult ValidateRows(IReadOnlyList<HyphenatedRow> rows)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<HyphenatedDomain> MapToDomain(IReadOnlyList<HyphenatedRow> rows, GSheetImportContext context)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ImportAsync(IEnumerable<HyphenatedDomain> domains, GSheetImportContext context)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class HyphenatedRow;

    private sealed class HyphenatedDomain;

    private sealed class PipelineWithoutAttribute : CoreLibs.LiveConfig.GSheet.Pipeline.IGSheetPipeline<HyphenatedRow, HyphenatedDomain>
    {
        public IEnumerable<HyphenatedDomain> MapToDomain(IReadOnlyList<HyphenatedRow> rows, GSheetImportContext context)
        {
            throw new NotSupportedException();
        }
    }
}
