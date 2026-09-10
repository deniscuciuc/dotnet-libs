using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Entity;

public record MappingTestChild(
    [property: GSheetColumn] string ChildId,
    [property: GSheetColumn] string ParentId,
    [property: GSheetColumn] int Order);

public class EntityMappingContextTests
{
    [Fact]
    public void Children_WithMatchingKey_ReturnsGrouped()
    {
        var importContext = new GSheetImportContext();
        importContext.SetDomain<MappingTestChild>(
        [
            new MappingTestChild("c1", "p1", 1),
            new MappingTestChild("c2", "p1", 2),
            new MappingTestChild("c3", "p2", 1)
        ]);

        // Build child groups using the relationship builder
        var relationship = new ChildRelationshipBuilder<MappingTestChild>();
        relationship.WithForeignKey<string>(c => c.ParentId);

        var groups = new Dictionary<Type, object>
        {
            [typeof(MappingTestChild)] = ((IChildRelationship)relationship).GroupChildren(importContext)
        };

        var ctx = new EntityMappingContext(importContext, groups);

        var children = ctx.Children<MappingTestChild>("p1");
        Assert.Equal(2, children.Count);
        Assert.All(children, c => Assert.Equal("p1", c.ParentId));
    }

    [Fact]
    public void Children_NoMatch_ReturnsEmpty()
    {
        var importContext = new GSheetImportContext();
        importContext.SetDomain<MappingTestChild>(
        [
            new MappingTestChild("c1", "p1", 1)
        ]);

        var relationship = new ChildRelationshipBuilder<MappingTestChild>();
        relationship.WithForeignKey<string>(c => c.ParentId);

        var groups = new Dictionary<Type, object>
        {
            [typeof(MappingTestChild)] = ((IChildRelationship)relationship).GroupChildren(importContext)
        };

        var ctx = new EntityMappingContext(importContext, groups);

        var children = ctx.Children<MappingTestChild>("nonexistent");
        Assert.Empty(children);
    }

    [Fact]
    public void Children_NoRelationship_ReturnsEmpty()
    {
        var importContext = new GSheetImportContext();
        var groups = new Dictionary<Type, object>();
        var ctx = new EntityMappingContext(importContext, groups);

        var children = ctx.Children<MappingTestChild>("p1");
        Assert.Empty(children);
    }

    [Fact]
    public void GetDomain_ReturnsFromImportContext()
    {
        var importContext = new GSheetImportContext();
        importContext.SetDomain(new List<string> { "a", "b" });

        var ctx = new EntityMappingContext(importContext, []);

        var result = ctx.GetDomain<string>();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ResolveRequired_WhenMissing_Throws()
    {
        var importContext = new GSheetImportContext();
        var ctx = new EntityMappingContext(importContext, []);

        Assert.Throws<InvalidOperationException>(() => ctx.ResolveRequired<int>());
    }

    [Fact]
    public void ChildRelationship_Ordered_SortsChildren()
    {
        var importContext = new GSheetImportContext();
        importContext.SetDomain<MappingTestChild>(
        [
            new MappingTestChild("c1", "p1", 3),
            new MappingTestChild("c2", "p1", 1),
            new MappingTestChild("c3", "p1", 2)
        ]);

        var relationship = new ChildRelationshipBuilder<MappingTestChild>();
        relationship.WithForeignKey<string>(c => c.ParentId).Ordered(c => c.Order);

        var groups = new Dictionary<Type, object>
        {
            [typeof(MappingTestChild)] = ((IChildRelationship)relationship).GroupChildren(importContext)
        };

        var ctx = new EntityMappingContext(importContext, groups);

        var children = ctx.Children<MappingTestChild>("p1");
        Assert.Equal(3, children.Count);
        Assert.Equal(1, children[0].Order);
        Assert.Equal(2, children[1].Order);
        Assert.Equal(3, children[2].Order);
    }
}
