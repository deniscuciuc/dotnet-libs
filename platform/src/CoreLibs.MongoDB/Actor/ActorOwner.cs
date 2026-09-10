using MongoDB.Bson;

namespace CoreLibs.MongoDB.Actor;

public class ActorOwner(ObjectId id)
{
    public ObjectId Id { get; } = id;
}
