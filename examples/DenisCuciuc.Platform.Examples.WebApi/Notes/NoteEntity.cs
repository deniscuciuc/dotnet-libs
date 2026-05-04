using DenisCuciuc.Platform.MongoDB;
using MongoDB.Bson.Serialization.Attributes;

namespace DenisCuciuc.Platform.Examples.WebApi.Notes;

public sealed class NoteEntity : EntityBase
{
    [BsonElement("title")]
    public required string Title { get; set; }

    [BsonElement("content")]
    public required string Content { get; set; }
}
