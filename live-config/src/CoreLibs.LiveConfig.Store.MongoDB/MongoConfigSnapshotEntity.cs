using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CoreLibs.LiveConfig.Store.MongoDB;

/// <summary>
/// MongoDB document representing a versioned config snapshot.
/// </summary>
public sealed class MongoConfigSnapshotEntity
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("configType")] public required string ConfigType { get; set; }

    [BsonElement("version")] public int Version { get; set; }

    [BsonElement("dataHash")] public required string DataHash { get; set; }

    [BsonElement("dataJson")] public required string DataJson { get; set; }

    [BsonElement("isActive")] public bool IsActive { get; set; }

    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("createdBy")] public string? CreatedBy { get; set; }
}
