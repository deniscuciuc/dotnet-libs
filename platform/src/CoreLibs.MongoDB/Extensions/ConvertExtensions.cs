using MongoDB.Bson;

namespace CoreLibs.MongoDB.Extensions;

public static class ConvertExtensions
{
    public static Guid ToGuid(this ObjectId id)
    {
        var bytes = new byte[16];
        id.ToByteArray(bytes, 0);
        return new Guid(bytes);
    }

    public static ObjectId ToObjectId(this Guid guid)
    {
        var bytes = new byte[12];
        Array.Copy(guid.ToByteArray(), bytes, bytes.Length);
        return new ObjectId(bytes);
    }
}
