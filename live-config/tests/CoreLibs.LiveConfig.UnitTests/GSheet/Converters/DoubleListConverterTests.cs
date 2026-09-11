using CoreLibs.LiveConfig.GSheet.Converters;
using CsvHelper;
using CsvHelper.Configuration;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Converters;

public class DoubleListConverterTests
{
    private readonly DoubleListConverter _converter = new();
    private readonly MemberMapData _mapData = new(null);

    private object? Convert(string? text)
    {
        var row = NSubstitute.Substitute.For<IReaderRow>();
        return _converter.ConvertFromString(text, row, _mapData);
    }

    [Fact]
    public void ConvertFromString_ValidDoubles()
    {
        var result = (List<double>)Convert("1.5, 2.3, 4.7")!;

        Assert.Equal(3, result.Count);
        Assert.Equal(1.5, result[0]);
        Assert.Equal(2.3, result[1]);
        Assert.Equal(4.7, result[2]);
    }

    [Fact]
    public void ConvertFromString_Empty_ReturnsEmptyList()
    {
        var result = (List<double>)Convert("")!;

        Assert.Empty(result);
    }

    [Fact]
    public void ConvertFromString_SingleValue()
    {
        var result = (List<double>)Convert("3.14")!;

        Assert.Single(result);
        Assert.Equal(3.14, result[0]);
    }
}
