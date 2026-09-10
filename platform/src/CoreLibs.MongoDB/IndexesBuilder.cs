using System.Linq.Expressions;
using MongoDB.Driver;

namespace CoreLibs.MongoDB;

public abstract class IndexesBuilder<TDocument>
{
    public List<IndexDefinition<TDocument>> Definitions { get; } = [];

    public FilterDefinitionBuilder<TDocument> Filter => Builders<TDocument>.Filter;

    protected CreateIndexOptionsBuilder<TDocument> Index(IndexKeysDefinition<TDocument> definition)
    {
        var options = new CreateIndexOptionsBuilder<TDocument>();

        Definitions.Add(new IndexDefinition<TDocument>(definition, options));

        return options;
    }

    protected IndexKeysDefinition<TDocument> Ascending(Expression<Func<TDocument, object>> field)
    {
        return Builders<TDocument>.IndexKeys.Ascending(field);
    }

    protected IndexKeysDefinition<TDocument> Ascending(FieldDefinition<TDocument> field)
    {
        return Builders<TDocument>.IndexKeys.Ascending(field);
    }

    protected IndexKeysDefinition<TDocument> Descending(Expression<Func<TDocument, object>> field)
    {
        return Builders<TDocument>.IndexKeys.Descending(field);
    }

    protected IndexKeysDefinition<TDocument> Descending(FieldDefinition<TDocument> field)
    {
        return Builders<TDocument>.IndexKeys.Descending(field);
    }

    protected IndexKeysDefinition<TDocument> Text(Expression<Func<TDocument, object>> field)
    {
        return Builders<TDocument>.IndexKeys.Text(field);
    }

    protected IndexKeysDefinition<TDocument> Text(FieldDefinition<TDocument> field)
    {
        return Builders<TDocument>.IndexKeys.Text(field);
    }

    public FilterDefinitionBuilder<T> FilterOf<T>()
    {
        return Builders<T>.Filter;
    }

    public ExpressionFilterDefinition<TDocument> Expression(Expression<Func<TDocument, bool>> expression)
    {
        return new ExpressionFilterDefinition<TDocument>(expression);
    }
}
