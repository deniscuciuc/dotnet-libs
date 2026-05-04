using System.Text.Json.Nodes;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace DenisCuciuc.Platform.MongoDB.Serializers;

public class JsonArrayBsonSerializer : SerializerBase<JsonArray>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonArray? value)
    {
        if (value == null)
        {
            context.Writer.WriteNull();
            return;
        }

        var bson = BsonDocument.Parse($"{{\"_array\":{value.ToJsonString()}}}")["_array"].AsBsonArray;
        BsonArraySerializer.Instance.Serialize(context, args, bson);
    }

    public override JsonArray Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null!;
        }

        var bson = BsonArraySerializer.Instance.Deserialize(context, args);
        var json = bson.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson });
        return JsonNode.Parse(json)!.AsArray();
    }
}
