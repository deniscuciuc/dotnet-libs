using MongoDB.Bson.Serialization.Attributes;

namespace CoreLibs.MongoDB;

/// <summary>
/// Entity with Compare-And-Swap (CAS) optimistic concurrency control.
/// </summary>
public abstract class EntityCas : EntityBase
{
    [BsonElement("_cas")] public long Cas { get; set; }
}
