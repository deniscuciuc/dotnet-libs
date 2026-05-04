using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB;

public abstract class RepositoryWithIndex<TDocument>(
    string collectionName,
    IMongoDBProvider provider,
    IndexesBuilder<TDocument> indexes
)
    : Repository<TDocument>(collectionName, provider), IRepositoryApplyIndex
{
    async Task IRepositoryApplyIndex.ApplyAsync(ILogger logger)
    {
        var definitions = indexes.Definitions;
        var indices = (await Collection.Indexes.ListAsync()).ToList();
        foreach (var index in indices) logger.LogInformation("[Index] Found {Name}", index["name"].AsString);

        var created = new List<(BsonDocument Document, CreateIndexModel<TDocument> Model)>();

        foreach (var (definition, options) in definitions)
        {
            var indexOptions = options.Options;

            var document = definition.Render(new RenderArgs<TDocument>(
                    Collection.DocumentSerializer,
                    BsonSerializer.SerializerRegistry
                )
            );

            indexOptions.Name ??= GenerateIndexName(document, indexOptions);

            var i = indices.FindIndex(x => x["name"].AsString == indexOptions.Name);
            if (i < 0)
                created.Add((document, new CreateIndexModel<TDocument>(definition, indexOptions)));
            else
                indices.RemoveAt(i);
        }

        try
        {
            foreach (var index in indices)
            {
                var name = index["name"].AsString;

                if (name == "_id_") continue;

                logger.LogInformation("[Index] Delete {Name}", index["key"]);
                await Collection.Indexes.DropOneAsync(name);
            }

            foreach (var (document, model) in created)
                logger.LogInformation("[Index] Create {Name} {Document}", model.Options.Name, document);

            if (created.Any()) await Collection.Indexes.CreateManyAsync(created.Select(x => x.Model));
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "[Index] Failed to create index");
            throw;
        }
    }

    private static string GenerateIndexName(BsonDocument document, CreateIndexOptions options)
    {
        var name = string.Join("_", document.Elements.Select(x => $"{x.Name}.{x.Value}"));

        if (options.Unique.HasValue) name += "_U";

        if (options.ExpireAfter.HasValue) name += $"_E{options.ExpireAfter.Value.TotalSeconds:F0}";

        return name;
    }
}
