using CoreLibs.LiveConfig.GSheet.Converters;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Converters;

public class RangeConverterTests
{
    private readonly RangeConverter _converter = new();
    private readonly MemberMapData _mapData = new(null);

    private object Convert(string? text)
    {
        var row = NSubstitute.Substitute.For<IReaderRow>();
        return _converter.ConvertFromString(text, row, _mapData);
    }

    [Fact]
    public void ConvertFromString_ValidRange()
    {
        var result = (NumericRange)Convert("1-10");

        Assert.Equal(1, result.Min);
        Assert.Equal(10, result.Max);
    }

    [Fact]
    public void ConvertFromString_DecimalRange()
    {
        var result = (NumericRange)Convert("0.5-99.9");

        Assert.Equal(0.5, result.Min);
        Assert.Equal(99.9, result.Max);
    }

    [Fact]
    public void ConvertFromString_NegativeMin()
    {
        var result = (NumericRange)Convert("-5-10");

        Assert.Equal(-5, result.Min);
        Assert.Equal(10, result.Max);
    }

    [Fact]
    public void ConvertFromString_Empty_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Convert(""));
    }

    [Fact]
    public void ConvertFromString_NoDash_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Convert("42"));
    }

    [Fact]
    public void NumericRange_Contains()
    {
        var range = new NumericRange(1, 10);

        Assert.True(range.Contains(5));
        Assert.True(range.Contains(1));
        Assert.True(range.Contains(10));
        Assert.False(range.Contains(0));
        Assert.False(range.Contains(11));
    }
}
