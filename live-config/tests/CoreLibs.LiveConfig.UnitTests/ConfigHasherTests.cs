namespace CoreLibs.LiveConfig.UnitTests;

public class ConfigHasherTests
{
    [Fact]
    public void ComputeHash_SameInput_ReturnsSameHash()
    {
        var hash1 = ConfigHasher.ComputeHash("hello world");
        var hash2 = ConfigHasher.ComputeHash("hello world");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentInput_ReturnsDifferentHash()
    {
        var hash1 = ConfigHasher.ComputeHash("hello");
        var hash2 = ConfigHasher.ComputeHash("world");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_Object_SerializesAndHashes()
    {
        var obj = new { Name = "test", Value = 42 };
        var hash = ConfigHasher.ComputeHash(obj);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void SerializeToJson_RoundTrips()
    {
        var original = new TestDto("test", 42);
        var json = ConfigHasher.SerializeToJson(original);
        var deserialized = ConfigHasher.DeserializeFromJson<TestDto>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Value, deserialized.Value);
    }

    private record TestDto(string Name, int Value);
}
