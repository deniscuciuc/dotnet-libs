using CoreLibs.LiveConfig.GSheet;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class GSheetImportContextTests
{
    [Fact]
    public void GetRequiredDomain_WhenDataExists_ReturnsList()
    {
        var ctx = new GSheetImportContext();
        ctx.SetDomain(new List<string> { "a", "b" });

        var result = ctx.GetRequiredDomain<string>();

        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0]);
    }

    [Fact]
    public void GetRequiredDomain_WhenMissing_Throws()
    {
        var ctx = new GSheetImportContext();

        var ex = Assert.Throws<InvalidOperationException>(() => ctx.GetRequiredDomain<int>());
        Assert.Contains("Int32", ex.Message);
    }

    [Fact]
    public void GetOptionalDomain_WhenMissing_ReturnsEmpty()
    {
        var ctx = new GSheetImportContext();

        var result = ctx.GetOptionalDomain<string>();

        Assert.Empty(result);
    }

    [Fact]
    public void GetOptionalDomain_WhenDataExists_ReturnsList()
    {
        var ctx = new GSheetImportContext();
        ctx.SetDomain(new List<string> { "x" });

        var result = ctx.GetOptionalDomain<string>();

        Assert.Single(result);
    }

    [Fact]
    public void GetGroupedDomain_GroupsByKey()
    {
        var ctx = new GSheetImportContext();
        ctx.SetDomain(new List<TestItem>
        {
            new("A", 1), new("A", 2), new("B", 3)
        });

        var groups = ctx.GetGroupedDomain<TestItem, string>(i => i.Group);

        Assert.Equal(2, groups["A"].Count());
        Assert.Single(groups["B"]);
    }

    [Fact]
    public void GetGroupedDomain_WhenEmpty_ReturnsEmptyLookup()
    {
        var ctx = new GSheetImportContext();

        var groups = ctx.GetGroupedDomain<TestItem, string>(i => i.Group);

        Assert.Empty(groups);
    }

    [Fact]
    public void GetIndexedDomain_IndexesByUniqueKey()
    {
        var ctx = new GSheetImportContext();
        ctx.SetDomain(new List<TestItem>
        {
            new("A", 1), new("B", 2)
        });

        var dict = ctx.GetIndexedDomain<TestItem, string>(i => i.Group);

        Assert.Equal(2, dict.Count);
        Assert.Equal(1, dict["A"].Value);
    }

    [Fact]
    public void GetIndexedDomain_ThrowsOnDuplicateKeys()
    {
        var ctx = new GSheetImportContext();
        ctx.SetDomain(new List<TestItem>
        {
            new("A", 1), new("A", 2)
        });

        Assert.Throws<ArgumentException>(() =>
            ctx.GetIndexedDomain<TestItem, string>(i => i.Group));
    }

    [Fact]
    public void Resolve_FindsByKey_CaseInsensitive()
    {
        var ctx = new GSheetImportContext();
        ctx.SetDomain(new List<TestItem>
        {
            new("Alpha", 1), new("Beta", 2)
        });

        var result = ctx.Resolve<TestItem>("alpha", i => i.Group);

        Assert.NotNull(result);
        Assert.Equal(1, result.Value);
    }

    [Fact]
    public void Resolve_WhenNotFound_ReturnsNull()
    {
        var ctx = new GSheetImportContext();
        ctx.SetDomain(new List<TestItem> { new("A", 1) });

        var result = ctx.Resolve<TestItem>("Z", i => i.Group);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveRequired_WhenFound_ReturnsEntity()
    {
        var ctx = new GSheetImportContext();
        ctx.SetDomain(new List<TestItem> { new("X", 42) });

        var result = ctx.ResolveRequired<TestItem>("X", i => i.Group);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ResolveRequired_WhenNotFound_Throws()
    {
        var ctx = new GSheetImportContext();
        ctx.SetDomain(new List<TestItem> { new("X", 1) });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ctx.ResolveRequired<TestItem>("missing", i => i.Group));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void SheetName_CanBeSetAndRead()
    {
        var ctx = new GSheetImportContext();
        ctx.SheetName = "TestSheet";

        Assert.Equal("TestSheet", ctx.SheetName);
    }

    [Fact]
    public void SheetName_DefaultsToNull()
    {
        var ctx = new GSheetImportContext();

        Assert.Null(ctx.SheetName);
    }

    private sealed record TestItem(string Group, int Value);
}
