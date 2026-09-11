namespace CoreLibs.Cache.Tests;

public class JsonCacheSerializerTests
{
    private readonly JsonCacheSerializer _serializer = new();

    [Fact]
    public void RoundTrip_String()
    {
        var bytes = _serializer.Serialize("hello");
        var result = _serializer.Deserialize<string>(bytes);

        Assert.Equal("hello", result);
    }

    [Fact]
    public void RoundTrip_ComplexObject()
    {
        var original = new TestDto { Id = 42, Name = "Test" };

        var bytes = _serializer.Serialize(original);
        var result = _serializer.Deserialize<TestDto>(bytes);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void RoundTrip_Null()
    {
        var bytes = _serializer.Serialize<string?>(null);
        var result = _serializer.Deserialize<string?>(bytes);

        Assert.Null(result);
    }

    private class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
