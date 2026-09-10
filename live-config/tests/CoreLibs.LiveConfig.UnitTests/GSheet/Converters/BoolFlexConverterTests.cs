using CoreLibs.LiveConfig.GSheet.Converters;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Converters;

public class BoolFlexConverterTests
{
    private readonly BoolFlexConverter _converter = new();
    private readonly MemberMapData _mapData = new(null);

    private object Convert(string? text)
    {
        var row = NSubstitute.Substitute.For<IReaderRow>();
        return _converter.ConvertFromString(text, row, _mapData);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("yes")]
    [InlineData("1")]
    [InlineData("on")]
    public void ConvertFromString_TrueValues(string input)
    {
        Assert.Equal(true, Convert(input));
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData("no")]
    [InlineData("0")]
    [InlineData("off")]
    public void ConvertFromString_FalseValues(string input)
    {
        Assert.Equal(false, Convert(input));
    }

    [Fact]
    public void ConvertFromString_Empty_ReturnsFalse()
    {
        Assert.Equal(false, Convert(""));
    }

    [Fact]
    public void ConvertFromString_InvalidValue_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Convert("maybe"));
    }
}
