using CoreLibs.LiveConfig.GSheet.Pipeline;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class CanonicalJsonSerializerTests
{
    [Fact]
    public void Serialize_SortsPropertiesAlphabetically()
    {
        var obj = new { Zebra = 1, Apple = 2, Mango = 3 };

        var json = CanonicalJsonSerializer.Serialize(obj, obj.GetType());

        // Properties should be sorted: apple, mango, zebra (camelCase)
        var appleIdx = json.IndexOf("\"apple\"");
        var mangoIdx = json.IndexOf("\"mango\"");
        var zebraIdx = json.IndexOf("\"zebra\"");

        Assert.True(appleIdx < mangoIdx);
        Assert.True(mangoIdx < zebraIdx);
    }

    [Fact]
    public void Serialize_DeterministicOutput_SameObjectSameResult()
    {
        var obj = new { Name = "Test", Value = 42, Active = true };

        var json1 = CanonicalJsonSerializer.Serialize(obj, obj.GetType());
        var json2 = CanonicalJsonSerializer.Serialize(obj, obj.GetType());

        Assert.Equal(json1, json2);
    }

    [Fact]
    public void Serialize_NestedObjects_SortedRecursively()
    {
        var obj = new
        {
            Outer = new { Zebra = 1, Apple = 2 },
            Name = "Test"
        };

        var json = CanonicalJsonSerializer.Serialize(obj, obj.GetType());

        // "name" should come before "outer", "apple" before "zebra" inside outer
        Assert.Contains("\"apple\"", json);
        Assert.Contains("\"zebra\"", json);
        var nameIdx = json.IndexOf("\"name\"");
        var outerIdx = json.IndexOf("\"outer\"");
        Assert.True(nameIdx < outerIdx);
    }

    [Fact]
    public void Serialize_Array_PresevesOrder()
    {
        var arr = new[] { 3, 1, 2 };

        var json = CanonicalJsonSerializer.Serialize(arr, arr.GetType());

        Assert.Equal("[3,1,2]", json);
    }

    [Fact]
    public void Serialize_NullsIgnored()
    {
        var obj = new { Name = "Test", Value = (string?)null };

        var json = CanonicalJsonSerializer.Serialize(obj, obj.GetType());

        Assert.DoesNotContain("value", json);
    }

    [Fact]
    public void Serialize_EmptyObject()
    {
        var obj = new { };

        var json = CanonicalJsonSerializer.Serialize(obj, obj.GetType());

        Assert.Equal("{}", json);
    }

    [Fact]
    public void Serialize_GenericOverload_Works()
    {
        var obj = new { Name = "Test", Count = 5 };

        var json = CanonicalJsonSerializer.Serialize(obj);

        Assert.Contains("\"count\"", json);
        Assert.Contains("\"name\"", json);
    }
}
