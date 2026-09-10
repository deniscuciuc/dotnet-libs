namespace CoreLibs.MongoDB.Exceptions;

public class EntityNotFoundException(Type type) : Exception($"Entity {type.Name} not found");
