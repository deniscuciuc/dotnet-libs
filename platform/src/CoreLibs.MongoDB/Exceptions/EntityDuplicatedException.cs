namespace CoreLibs.MongoDB.Exceptions;

public class EntityDuplicatedException(Type type) : Exception($"Entity {type.Name} duplicated");
