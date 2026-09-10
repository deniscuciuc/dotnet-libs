using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Converters;

public class DateOnlyConverterTests
{
    private readonly CoreLibs.LiveConfig.GSheet.Converters.DateOnlyConverter _converter = new();
    private readonly MemberMapData _mapData = new(null);

    private object Convert(string? text)
    {
        var row = NSubstitute.Substitute.For<IReaderRow>();
        return _converter.ConvertFromString(text, row, _mapData);
    }

    [Fact]
    public void ConvertFromString_ValidDate()
    {
        var result = (DateOnly)Convert("2025-01-15");

        Assert.Equal(new DateOnly(2025, 1, 15), result);
    }

    [Fact]
    public void ConvertFromString_InvalidDate_Throws()
    {
        Assert.ThrowsAny<Exception>(() => Convert("not-a-date"));
    }
}
