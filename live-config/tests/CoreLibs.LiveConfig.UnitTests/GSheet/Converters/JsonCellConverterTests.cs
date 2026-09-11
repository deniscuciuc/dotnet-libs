using CoreLibs.LiveConfig.GSheet.Converters;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Converters;

public class JsonCellConverterTests
{
    private readonly MemberMapData _mapData = new(null);

    private object? Convert<T>(string? text)
    {
        var converter = new JsonCellConverter<T>();
        var row = NSubstitute.Substitute.For<IReaderRow>();
        return converter.ConvertFromString(text, row, _mapData);
    }

    public class TestPayload
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    [Fact]
    public void ConvertFromString_ValidJson()
    {
        var result = (TestPayload?)Convert<TestPayload>("""{"name":"Test","value":42}""");

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ConvertFromString_Empty_ReturnsNull()
    {
        var result = Convert<TestPayload>("");

        Assert.Null(result);
    }

    [Fact]
    public void ConvertFromString_InvalidJson_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Convert<TestPayload>("not json"));
    }

    [Fact]
    public void ConvertFromString_Array()
    {
        var result = (int[]?)Convert<int[]>("[1,2,3]");

        Assert.NotNull(result);
        Assert.Equal([1, 2, 3], result);
    }
}
