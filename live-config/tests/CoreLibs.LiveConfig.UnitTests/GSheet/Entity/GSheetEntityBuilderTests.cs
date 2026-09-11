using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Entity;

// ── Test types ────────────────────────────────────

[GSheetEntity("TestSheet", "A1:C")]
public record SimpleEntity(
    [property: GSheetColumn]
    [property: GSheetRequired]
    string Id,
    [property: GSheetColumn] string Name,
    [property: GSheetColumn]
    [property: GSheetRange(0, 100)]
    int Value);

public class GSheetEntityBuilderTests
{
    [Fact]
    public void Builder_DefaultIdentityMapping_MapsRowToSelf()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<SimpleEntity> { new("x", "Item", 10) };

        var result = adapter.MapToDomain(rows, new GSheetImportContext()).ToList();

        Assert.Single(result);
        Assert.Equal("x", result[0].Id);
    }

    [Fact]
    public void Builder_OrderBy_SortsRows()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        builder.OrderBy(x => x.Name);

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<SimpleEntity>
        {
            new("2", "Banana", 1),
            new("1", "Apple", 2),
            new("3", "Cherry", 3)
        };
        var context = new GSheetImportContext();

        // Transformer should sort
        var transformed = adapter.Transformer!.Transform(rows, context);

        Assert.Equal("Apple", transformed[0].Name);
        Assert.Equal("Banana", transformed[1].Name);
        Assert.Equal("Cherry", transformed[2].Name);
    }

    [Fact]
    public void Builder_ThenBy_SortsMultipleKeys()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        builder.OrderBy(x => x.Name).ThenBy(x => x.Value);

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<SimpleEntity>
        {
            new("1", "A", 30),
            new("2", "A", 10),
            new("3", "B", 5)
        };

        var transformed = adapter.Transformer!.Transform(rows, new GSheetImportContext());

        Assert.Equal(10, transformed[0].Value);
        Assert.Equal(30, transformed[1].Value);
        Assert.Equal(5, transformed[2].Value);
    }

    [Fact]
    public void Builder_CustomTransform_Applied()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        builder.Transform(rows => rows.Where(r => r.Value > 5).ToList());

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<SimpleEntity>
        {
            new("1", "Low", 3),
            new("2", "High", 50)
        };

        var transformed = adapter.Transformer!.Transform(rows, new GSheetImportContext());

        Assert.Single(transformed);
        Assert.Equal("High", transformed[0].Name);
    }

    [Fact]
    public void Builder_DependsOn_RecordsExplicitDependency()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        builder.DependsOn<string>();

        Assert.Single(builder.ExplicitDependencies);
        Assert.Equal(typeof(string), builder.ExplicitDependencies[0]);
    }

    [Fact]
    public void Builder_Sheet_SetsMetadata()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity, SimpleEntity>();
        builder.Sheet("MySheet", "B2:G");

        Assert.Equal("MySheet", builder.SheetName);
        Assert.Equal("B2:G", builder.Range);
    }

    [Fact]
    public void Builder_Sheet_InvalidRange_Throws()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity, SimpleEntity>();

        Assert.Throws<ArgumentException>(() => builder.Sheet("Sheet", "invalid!!!"));
    }

    [Fact]
    public void Builder_NoRowValidator_WhenNoRules()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        var adapter = builder.Build("TestSheet", "A1:C");

        Assert.Null(adapter.RowValidator);
    }

    [Fact]
    public void Builder_NoGraphValidator_WhenNoGraphRules()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        var adapter = builder.Build("TestSheet", "A1:C");

        Assert.Null(adapter.GraphValidator);
    }

    [Fact]
    public void Builder_NoTransformer_WhenNoOrderingOrTransform()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        var adapter = builder.Build("TestSheet", "A1:C");

        Assert.Null(adapter.Transformer);
    }
}
