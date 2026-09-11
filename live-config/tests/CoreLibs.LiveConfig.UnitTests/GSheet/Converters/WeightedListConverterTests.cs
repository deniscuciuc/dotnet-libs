using CoreLibs.LiveConfig.GSheet.Converters;
using CsvHelper;
using CsvHelper.Configuration;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Converters;

public class WeightedListConverterTests
{
    private readonly WeightedListConverter _converter = new();
    private readonly MemberMapData _mapData = new(null);

    private object Convert(string? text)
    {
        var row = NSubstitute.Substitute.For<IReaderRow>();
        return _converter.ConvertFromString(text, row, _mapData);
    }

    [Fact]
    public void ConvertFromString_ValidWeights()
    {
        var result = (List<WeightedItem<string>>)Convert("gold:50,xp:30");

        Assert.Equal(2, result.Count);
        Assert.Equal("gold", result[0].Key);
        Assert.Equal(50, result[0].Weight);
        Assert.Equal("xp", result[1].Key);
        Assert.Equal(30, result[1].Weight);
    }

    [Fact]
    public void ConvertFromString_Empty_ReturnsEmptyList()
    {
        var result = (List<WeightedItem<string>>)Convert("");

        Assert.Empty(result);
    }

    [Fact]
    public void ConvertFromString_SingleItem()
    {
        var result = (List<WeightedItem<string>>)Convert("gold:100");

        Assert.Single(result);
        Assert.Equal("gold", result[0].Key);
        Assert.Equal(100, result[0].Weight);
    }

    [Fact]
    public void ConvertFromString_DecimalWeights()
    {
        var result = (List<WeightedItem<string>>)Convert("rare:0.5,common:99.5");

        Assert.Equal(0.5, result[0].Weight);
        Assert.Equal(99.5, result[1].Weight);
    }
}
