using CoreLibs.Localization;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace CoreLibs.Localization.Store.MongoDB.Serializers;

public sealed class LanguageCodeSerializer : StructSerializerBase<LanguageCode>
{
    public static readonly LanguageCodeSerializer Default = new();

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, LanguageCode value)
    {
        context.Writer.WriteString(value);
    }

    public override LanguageCode Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return context.Reader.ReadString();
    }
}
