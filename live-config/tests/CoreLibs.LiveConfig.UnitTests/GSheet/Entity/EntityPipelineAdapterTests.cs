using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Entity;

// ── Test domain types ─────────────────────────────

public record ParentRow(
    [property: GSheetColumn] string ParentId,
    [property: GSheetColumn] string Name);

public record ChildRow(
    [property: GSheetColumn] string ChildId,
    [property: GSheetColumn] string ParentId,
    [property: GSheetColumn] int SortOrder);

public record ParentDomain(string ParentId, string Name, IReadOnlyList<ChildRow> Children);

public class EntityPipelineAdapterTests
{
    [Fact]
    public void Tier1_IdentityMapping_RowIsReturned()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        var adapter = builder.Build("TestSheet", "A1:C");

        var rows = new List<SimpleEntity> { new("1", "Test", 42) };
        var result = adapter.MapToDomain(rows, new GSheetImportContext()).ToList();

        Assert.Single(result);
        Assert.Equal("1", result[0].Id);
        Assert.Equal(42, result[0].Value);
    }

    [Fact]
    public void Tier1_ImplementsIGSheetPipeline()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        var adapter = builder.Build("TestSheet", "A1:C");

        Assert.IsAssignableFrom<IGSheetPipeline<SimpleEntity, SimpleEntity>>(adapter);
    }

    [Fact]
    public void Adapter_ExposesSheetMetadata()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        var adapter = builder.Build("TestSheet", "A1:C");

        Assert.Equal("TestSheet", adapter.EntitySheetName);
        Assert.Equal("A1:C", adapter.EntityRange);
    }

    [Fact]
    public void Tier3_MapWithContext_UsesChildRelationship()
    {
        var builder = new GSheetEntityBuilder<ParentRow, ParentDomain>();
        builder.Sheet("Parents", "A1:B");

        builder.HasMany<ChildRow>()
            .WithForeignKey(c => c.ParentId)
            .Ordered(c => c.SortOrder);

        builder.Map((row, ctx) => new ParentDomain(
            row.ParentId,
            row.Name,
            ctx.Children<ChildRow>(row.ParentId)));

        var adapter = builder.Build("Parents", "A1:B");

        // Set up import context with child data
        var context = new GSheetImportContext();
        context.SetDomain<ChildRow>(
        [
            new ChildRow("c1", "p1", 2),
            new ChildRow("c2", "p1", 1),
            new ChildRow("c3", "p2", 1)
        ]);

        var rows = new List<ParentRow>
        {
            new("p1", "Parent 1"),
            new("p2", "Parent 2")
        };

        var result = adapter.MapToDomain(rows, context).ToList();

        Assert.Equal(2, result.Count);

        // p1 should have 2 children, ordered by SortOrder
        Assert.Equal(2, result[0].Children.Count);
        Assert.Equal("c2", result[0].Children[0].ChildId); // SortOrder 1
        Assert.Equal("c1", result[0].Children[1].ChildId); // SortOrder 2

        // p2 should have 1 child
        Assert.Single(result[1].Children);
    }

    [Fact]
    public void Tier3_MapWithContext_EmptyChildren_ReturnsEmptyList()
    {
        var builder = new GSheetEntityBuilder<ParentRow, ParentDomain>();
        builder.Sheet("Parents", "A1:B");
        builder.HasMany<ChildRow>().WithForeignKey(c => c.ParentId);
        builder.Map((row, ctx) => new ParentDomain(row.ParentId, row.Name, ctx.Children<ChildRow>(row.ParentId)));

        var adapter = builder.Build("Parents", "A1:B");
        var context = new GSheetImportContext();
        // No child data set → should get empty lists

        var rows = new List<ParentRow> { new("p1", "Parent 1") };
        var result = adapter.MapToDomain(rows, context).ToList();

        Assert.Single(result);
        Assert.Empty(result[0].Children);
    }

    [Fact]
    public void MapSimple_UsedInTier2Config()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity, string>();
        builder.MapSimple(row => $"{row.Id}:{row.Name}");

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<SimpleEntity> { new("1", "Foo", 10) };

        var result = adapter.MapToDomain(rows, new GSheetImportContext()).ToList();

        Assert.Equal("1:Foo", result[0]);
    }

    [Fact]
    public async Task ProcessAsync_DefaultReturnsSuccess()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        var adapter = builder.Build("TestSheet", "A1:C");

        var result = await adapter.ProcessAsync([new SimpleEntity("1", "Test", 0)], new GSheetImportContext());

        Assert.False(result.HasErrors);
        Assert.Single(result.Data);
    }

    [Fact]
    public void InlineValidation_ProducesErrors()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        builder.Validate(rows =>
            rows.Where(r => r.Value < 0).Select(r => $"Value {r.Value} is negative"));

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<SimpleEntity> { new("1", "Bad", -5) };

        var result = adapter.RowValidator!.Validate(rows, new GSheetImportContext());

        Assert.True(result.HasErrors);
        Assert.Contains("negative", result.Errors[0].Reason);
    }

    [Fact]
    public void GraphValidation_ProducesErrors()
    {
        var builder = new GSheetEntityBuilder<SimpleEntity>();
        builder.ValidateGraph((rows, ctx) =>
        {
            var dupes = rows.GroupBy(r => r.Id).Where(g => g.Count() > 1);
            return dupes.Select(g => $"Duplicate Id: {g.Key}");
        });

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<SimpleEntity>
        {
            new("1", "A", 1),
            new("1", "B", 2)
        };

        var result = adapter.GraphValidator!.Validate(rows, new GSheetImportContext());

        Assert.True(result.HasErrors);
        Assert.Contains("Duplicate Id: 1", result.Errors[0].Reason);
    }
}
