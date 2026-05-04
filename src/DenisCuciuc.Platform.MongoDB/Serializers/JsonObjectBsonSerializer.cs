using System.Text.Json.Nodes;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace DenisCuciuc.Platform.MongoDB.Serializers;

public class JsonObjectBsonSerializer : SerializerBase<JsonObject>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonObject? value)
    {
        if (value == null)
        {
            context.Writer.WriteNull();
            return;
        }

        var bson = BsonDocument.Parse(value.ToJsonString());
        BsonDocumentSerializer.Instance.Serialize(context, args, bson);
    }

    public override JsonObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null!;
        }

        var bson = BsonDocumentSerializer.Instance.Deserialize(context, args);
        var json = bson.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson });
        return JsonNode.Parse(json)!.AsObject();
    }
}
