using MongoDB.Driver;

namespace CoreLibs.MongoDB;

public record IndexDefinition<TDocument>(
    IndexKeysDefinition<TDocument> Definition,
    CreateIndexOptionsBuilder<TDocument> Options
);
