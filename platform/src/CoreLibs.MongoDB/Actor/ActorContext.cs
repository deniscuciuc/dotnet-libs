namespace CoreLibs.MongoDB.Actor;

public readonly struct ActorContext(ActorOwner? owner, string identity)
{
    public ActorOwner? Owner { get; } = owner;
    public string Identity { get; } = identity;
}
