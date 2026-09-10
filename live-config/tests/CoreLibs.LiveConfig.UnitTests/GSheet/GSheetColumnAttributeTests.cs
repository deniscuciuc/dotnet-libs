using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class GSheetColumnAttributeTests
{
    [Fact]
    public void Parameterless_Constructor_SetsNameToNull()
    {
        var attr = new GSheetColumnAttribute();

        Assert.Null(attr.Name);
    }

    [Fact]
    public void Named_Constructor_SetsName()
    {
        var attr = new GSheetColumnAttribute("MyColumn");

        Assert.Equal("MyColumn", attr.Name);
    }

    [Fact]
    public void AttributeUsage_AllowsPropertyTarget()
    {
        var usage = typeof(GSheetColumnAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>()
            .FirstOrDefault();

        Assert.NotNull(usage);
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Property));
    }
}
