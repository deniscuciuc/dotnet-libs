using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Converters;

public class GuidConverterTests
{
    private readonly CoreLibs.LiveConfig.GSheet.Converters.GuidConverter _converter = new();
    private readonly MemberMapData _mapData = new(null);

    private object Convert(string? text)
    {
        var row = NSubstitute.Substitute.For<IReaderRow>();
        return _converter.ConvertFromString(text, row, _mapData);
    }

    [Fact]
    public void ConvertFromString_ValidGuid()
    {
        var guid = Guid.NewGuid();
        var result = (Guid)Convert(guid.ToString());

        Assert.Equal(guid, result);
    }

    [Fact]
    public void ConvertFromString_InvalidGuid_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Convert("not-a-guid"));
    }
}
