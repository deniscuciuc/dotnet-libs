namespace DenisCuciuc.Platform.MongoDB.Exceptions;

public class EntityNotFoundException(Type type) : Exception($"Entity {type.Name} not found");
