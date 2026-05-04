using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB;

public record IndexDefinition<TDocument>(
    IndexKeysDefinition<TDocument> Definition,
    CreateIndexOptionsBuilder<TDocument> Options
);
