using MongoDB.Bson.Serialization.Attributes;

namespace DenisCuciuc.Platform.MongoDB;

/// <summary>
/// Entity with Compare-And-Swap (CAS) optimistic concurrency control.
/// </summary>
public abstract class EntityCas : EntityBase
{
    [BsonElement("_cas")] public long Cas { get; set; }
}
