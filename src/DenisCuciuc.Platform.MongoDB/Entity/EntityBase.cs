using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DenisCuciuc.Platform.MongoDB;

[BsonIgnoreExtraElements(Inherited = true)]
public abstract class EntityBase
{
    private string? _stringId;

    protected EntityBase()
    {
        var now = DateTime.UtcNow;

        Id = ObjectId.GenerateNewId(now);
        CreatedAt = now;
        ModifiedAt = now;
    }

    [BsonId(Order = 1)] public ObjectId Id { get; set; }

    [BsonElement("_created", Order = 2)] public DateTime CreatedAt { get; set; }

    [BsonElement("_modified", Order = 3)] public DateTime ModifiedAt { get; set; }

    [BsonIgnore] public string StringId => _stringId ??= Id.ToString();

    public TimeSpan GetLifetime()
    {
        return DateTime.UtcNow - CreatedAt;
    }
}
