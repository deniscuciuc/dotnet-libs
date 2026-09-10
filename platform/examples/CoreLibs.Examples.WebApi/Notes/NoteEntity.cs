using CoreLibs.MongoDB;
using MongoDB.Bson.Serialization.Attributes;

namespace CoreLibs.Examples.WebApi.Notes;

public sealed class NoteEntity : EntityBase
{
    [BsonElement("title")]
    public required string Title { get; set; }

    [BsonElement("content")]
    public required string Content { get; set; }
}
