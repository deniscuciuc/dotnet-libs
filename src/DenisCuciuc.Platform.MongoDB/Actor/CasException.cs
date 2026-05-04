namespace DenisCuciuc.Platform.MongoDB.Actor;

public class CasException(EntityCas entity) : Exception
{
    public EntityCas Entity { get; } = entity;
}
