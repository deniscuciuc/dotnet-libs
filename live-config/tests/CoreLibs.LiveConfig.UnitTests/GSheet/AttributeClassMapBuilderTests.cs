using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class AttributeClassMapBuilderTests
{
    // --- Test row types ---

    public class AnnotatedRow
    {
        [GSheetColumn("Item Name")] public string Name { get; set; } = "";

        [GSheetColumn("Qty", Order = 1)] public int Quantity { get; set; }

        [GSheetColumn("Price")]
        [GSheetDefault(9.99)]
        public double Price { get; set; }

        [GSheetIgnore] public string Internal { get; set; } = "";
    }

    public class ConventionRow
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public int Count { get; set; }
    }

    public class MixedRow
    {
        [GSheetColumn("Code")] public string Code { get; set; } = "";

        // No attribute — should be skipped when other props have [GSheetColumn]
        public string Unmapped { get; set; } = "";
    }

    // --- Build tests ---

    [Fact]
    public void Build_AnnotatedRow_MapsByColumnAttribute()
    {
        var map = AttributeClassMapBuilder.Build<AnnotatedRow>();
        var members = map.MemberMaps.ToList();

        // Should map 3 properties (Name, Quantity, Price) — not Internal (ignored)
        Assert.Equal(3, members.Count);
        Assert.Contains(members, m => m.Data.Names.Contains("Item Name"));
        Assert.Contains(members, m => m.Data.Names.Contains("Qty"));
        Assert.Contains(members, m => m.Data.Names.Contains("Price"));
    }

    [Fact]
    public void Build_AnnotatedRow_SetsOrder()
    {
        var map = AttributeClassMapBuilder.Build<AnnotatedRow>();
        var qty = map.MemberMaps.First(m => m.Data.Names.Contains("Qty"));
        Assert.Equal(1, qty.Data.Index);
    }

    [Fact]
    public void Build_ConventionRow_AutoMapsByPropertyName()
    {
        var map = AttributeClassMapBuilder.Build<ConventionRow>();
        var members = map.MemberMaps.ToList();

        Assert.Equal(3, members.Count);
        Assert.Contains(members, m => m.Data.Names.Contains("Id"));
        Assert.Contains(members, m => m.Data.Names.Contains("Label"));
        Assert.Contains(members, m => m.Data.Names.Contains("Count"));
    }

    [Fact]
    public void Build_MixedRow_OnlyMapsAnnotatedProperties()
    {
        var map = AttributeClassMapBuilder.Build<MixedRow>();
        var members = map.MemberMaps.ToList();

        Assert.Single(members);
        Assert.Contains(members, m => m.Data.Names.Contains("Code"));
    }

    // --- GetExpectedColumns tests ---

    [Fact]
    public void GetExpectedColumns_AnnotatedRow_ReturnsAttributeNames()
    {
        var columns = AttributeClassMapBuilder.GetExpectedColumns<AnnotatedRow>();

        Assert.Equal(3, columns.Count);
        Assert.Contains("Item Name", columns);
        Assert.Contains("Qty", columns);
        Assert.Contains("Price", columns);
        Assert.DoesNotContain("Internal", columns);
    }

    [Fact]
    public void GetExpectedColumns_ConventionRow_ReturnsPropertyNames()
    {
        var columns = AttributeClassMapBuilder.GetExpectedColumns<ConventionRow>();

        Assert.Equal(3, columns.Count);
        Assert.Contains("Id", columns);
        Assert.Contains("Label", columns);
        Assert.Contains("Count", columns);
    }

    [Fact]
    public void GetExpectedColumns_MixedRow_OnlyAnnotated()
    {
        var columns = AttributeClassMapBuilder.GetExpectedColumns<MixedRow>();

        Assert.Single(columns);
        Assert.Contains("Code", columns);
    }
}
