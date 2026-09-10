using CoreLibs.LiveConfig.GSheet.Converters;
using CsvHelper;
using CsvHelper.Configuration;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Converters;

public class DictionaryConverterTests
{
    private readonly DictionaryConverter _converter = new();
    private readonly MemberMapData _mapData = new(null);

    private object Convert(string? text)
    {
        var row = NSubstitute.Substitute.For<IReaderRow>();
        return _converter.ConvertFromString(text, row, _mapData);
    }

    [Fact]
    public void ConvertFromString_ValidPairs()
    {
        var result = (Dictionary<string, string>)Convert("key1:val1,key2:val2");

        Assert.Equal(2, result.Count);
        Assert.Equal("val1", result["key1"]);
        Assert.Equal("val2", result["key2"]);
    }

    [Fact]
    public void ConvertFromString_Empty_ReturnsEmptyDict()
    {
        var result = (Dictionary<string, string>)Convert("");

        Assert.Empty(result);
    }

    [Fact]
    public void ConvertFromString_SinglePair()
    {
        var result = (Dictionary<string, string>)Convert("name:test");

        Assert.Single(result);
        Assert.Equal("test", result["name"]);
    }

    [Fact]
    public void ConvertFromString_CaseInsensitiveKeys()
    {
        var result = (Dictionary<string, string>)Convert("Name:test");

        Assert.True(result.ContainsKey("name"));
        Assert.True(result.ContainsKey("NAME"));
    }
}
