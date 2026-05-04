using MongoDB.Bson;

namespace DenisCuciuc.Platform.MongoDB.Actor;

public class ActorOwner(ObjectId id)
{
    public ObjectId Id { get; } = id;
}
