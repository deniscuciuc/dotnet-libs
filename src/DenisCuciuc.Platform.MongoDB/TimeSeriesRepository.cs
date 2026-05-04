using System.Linq.Expressions;
using MongoDB.Driver;

namespace DenisCuciuc.Platform.MongoDB;

public abstract class TimeSeriesRepository<TDocument> : Repository<TDocument>
{
    protected TimeSeriesRepository(
        string collectionName,
        IMongoDBProvider provider,
        Expression<Func<TDocument, DateTime>> timeField,
        Expression<Func<TDocument, object?>>? metaField = null,
        TimeSeriesGranularity? granularity = null,
        TimeSpan? expireAfter = null
    ) : base(collectionName, provider)
    {
        TimeFieldName = GetMemberPath(timeField) ??
                        throw new ArgumentException("Time field expression must point to a member", nameof(timeField));
        MetaFieldName = metaField != null ? GetMemberPath(metaField) : null;
        Granularity = granularity;
        ExpireAfter = expireAfter;

        EnsureTimeSeriesCollectionExists(collectionName, Database);
    }

    protected TimeSeriesRepository(
        string collectionName,
        IMongoDBProvider provider,
        Expression<Func<TDocument, DateTime?>> timeField,
        Expression<Func<TDocument, object?>>? metaField = null,
        TimeSeriesGranularity? granularity = null,
        TimeSpan? expireAfter = null
    ) : base(collectionName, provider)
    {
        TimeFieldName = GetMemberPath(timeField) ??
                        throw new ArgumentException("Time field expression must point to a member", nameof(timeField));
        MetaFieldName = metaField != null ? GetMemberPath(metaField) : null;
        Granularity = granularity;
        ExpireAfter = expireAfter;

        EnsureTimeSeriesCollectionExists(collectionName, Database);
    }

    protected TimeSeriesRepository(
        string collectionName,
        IMongoDBProvider provider,
        string timeFieldName,
        string? metaFieldName = null,
        TimeSeriesGranularity? granularity = null,
        TimeSpan? expireAfter = null
    ) : base(collectionName, provider)
    {
        if (string.IsNullOrWhiteSpace(timeFieldName))
            throw new ArgumentException("Time field name is required", nameof(timeFieldName));

        TimeFieldName = timeFieldName;
        MetaFieldName = string.IsNullOrWhiteSpace(metaFieldName) ? null : metaFieldName;
        Granularity = granularity;
        ExpireAfter = expireAfter;

        EnsureTimeSeriesCollectionExists(collectionName, Database);
    }

    protected string TimeFieldName { get; }
    protected string? MetaFieldName { get; }
    protected TimeSpan? ExpireAfter { get; }
    protected TimeSeriesGranularity? Granularity { get; }

    protected virtual void ConfigureCreateCollectionOptions(CreateCollectionOptions options)
    {
    }

    private void EnsureTimeSeriesCollectionExists(string collectionName, IMongoDatabase database)
    {
        var tsOptions = new TimeSeriesOptions(TimeFieldName, MetaFieldName, Granularity);

        var createOptions = new CreateCollectionOptions
        {
            TimeSeriesOptions = tsOptions,
            ExpireAfter = ExpireAfter
        };

        ConfigureCreateCollectionOptions(createOptions);

        try
        {
            database.CreateCollection(collectionName, createOptions);
        }
        catch (MongoCommandException mce)
        {
            if (!IsNamespaceExistsError(mce)) throw;
        }
    }

    private static bool IsNamespaceExistsError(MongoCommandException ex)
    {
        return ex.Code == 48 || ex.Message.Contains("NamespaceExists", StringComparison.OrdinalIgnoreCase);
    }

    protected FieldDefinition<TDocument, DateTime> TimeField()
    {
        return new StringFieldDefinition<TDocument, DateTime>(TimeFieldName);
    }

    protected FilterDefinition<TDocument> TimeBetween(DateTime startInclusive, DateTime endExclusive)
    {
        var fb = Builders<TDocument>.Filter;
        return fb.And(
            fb.Gte(TimeFieldName, startInclusive),
            fb.Lt(TimeFieldName, endExclusive)
        );
    }

    protected FilterDefinition<TDocument> TimeAfter(DateTime startInclusive)
    {
        return Builders<TDocument>.Filter.Gte(TimeFieldName, startInclusive);
    }

    protected FilterDefinition<TDocument> TimeBefore(DateTime endExclusive)
    {
        return Builders<TDocument>.Filter.Lt(TimeFieldName, endExclusive);
    }

    protected FilterDefinition<TDocument> MetaEquals(object? meta)
    {
        return string.IsNullOrEmpty(MetaFieldName)
            ? Builders<TDocument>.Filter.Empty
            : Builders<TDocument>.Filter.Eq(MetaFieldName!, meta);
    }


    public Task InsertAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        return Collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public Task InsertManyAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken = default)
    {
        return Collection.InsertManyAsync(documents, cancellationToken: cancellationToken);
    }

    public async Task<List<TDocument>> GetRangeAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        FilterDefinition<TDocument>? extraFilter = null,
        ProjectionDefinition<TDocument>? projection = null,
        SortDefinition<TDocument>? sort = null,
        int? limit = null,
        bool readFromSecondary = true,
        CancellationToken cancellationToken = default
    )
    {
        var filter = TimeBetween(startInclusive, endExclusive);
        if (extraFilter != null) filter = Builders<TDocument>.Filter.And(filter, extraFilter);

        var coll = readFromSecondary ? Secondary : Primary;
        var find = coll.Find(filter, new FindOptions());

        if (projection != null) find = find.Project<TDocument>(projection);

        find = find.Sort(sort ?? Builders<TDocument>.Sort.Ascending(TimeFieldName));

        if (limit.HasValue) find = find.Limit(limit.Value);

        return await find.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<TDocument>> GetRangeByMetaAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        object? meta,
        FilterDefinition<TDocument>? extraFilter = null,
        ProjectionDefinition<TDocument>? projection = null,
        SortDefinition<TDocument>? sort = null,
        int? limit = null,
        bool readFromSecondary = true,
        CancellationToken cancellationToken = default
    )
    {
        var filter = Builders<TDocument>.Filter.And(TimeBetween(startInclusive, endExclusive), MetaEquals(meta));
        if (extraFilter != null) filter = Builders<TDocument>.Filter.And(filter, extraFilter);
        var coll = readFromSecondary ? Secondary : Primary;
        var find = coll.Find(filter, new FindOptions());
        if (projection != null) find = find.Project<TDocument>(projection);
        find = find.Sort(sort ?? Builders<TDocument>.Sort.Ascending(TimeFieldName));
        if (limit.HasValue) find = find.Limit(limit.Value);
        return await find.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<long> CountAsync(
        DateTime? startInclusive = null,
        DateTime? endExclusive = null,
        FilterDefinition<TDocument>? extraFilter = null,
        bool readFromSecondary = true,
        CancellationToken cancellationToken = default
    )
    {
        var filter = Builders<TDocument>.Filter.Empty;
        if (startInclusive.HasValue || endExclusive.HasValue)
        {
            var fb = Builders<TDocument>.Filter;
            var parts = new List<FilterDefinition<TDocument>>();
            if (startInclusive.HasValue) parts.Add(fb.Gte(TimeFieldName, startInclusive.Value));
            if (endExclusive.HasValue) parts.Add(fb.Lt(TimeFieldName, endExclusive.Value));
            filter = fb.And(parts);
        }

        if (extraFilter != null) filter = Builders<TDocument>.Filter.And(filter, extraFilter);

        var coll = readFromSecondary ? Secondary : Primary;
        return coll.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public Task<long> CountByMetaAsync(
        object? meta,
        DateTime? startInclusive = null,
        DateTime? endExclusive = null,
        FilterDefinition<TDocument>? extraFilter = null,
        bool readFromSecondary = true,
        CancellationToken cancellationToken = default
    )
    {
        var fb = Builders<TDocument>.Filter;
        var parts = new List<FilterDefinition<TDocument>> { MetaEquals(meta) };
        if (startInclusive.HasValue) parts.Add(fb.Gte(TimeFieldName, startInclusive.Value));
        if (endExclusive.HasValue) parts.Add(fb.Lt(TimeFieldName, endExclusive.Value));
        var filter = fb.And(parts);
        if (extraFilter != null) filter = fb.And(filter, extraFilter);
        var coll = readFromSecondary ? Secondary : Primary;
        return coll.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<TDocument?> GetLatestAsync(
        FilterDefinition<TDocument>? filter = null,
        bool readFromSecondary = true,
        CancellationToken cancellationToken = default
    )
    {
        var fb = Builders<TDocument>.Filter;
        var f = filter ?? fb.Empty;

        var doc = await (readFromSecondary ? Secondary : Primary)
            .Find(f)
            .Sort(Builders<TDocument>.Sort.Descending(TimeFieldName))
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return doc;
    }

    public Task<TDocument?> GetLatestByMetaAsync(
        object? meta,
        FilterDefinition<TDocument>? extraFilter = null,
        bool readFromSecondary = true,
        CancellationToken cancellationToken = default
    )
    {
        var filter = extraFilter == null
            ? MetaEquals(meta)
            : Builders<TDocument>.Filter.And(MetaEquals(meta), extraFilter);
        return (readFromSecondary ? Secondary : Primary)
            .Find(filter)
            .Sort(Builders<TDocument>.Sort.Descending(TimeFieldName))
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken)!;
    }

    public async Task<long> DeleteOlderThanAsync(DateTime thresholdExclusive,
        CancellationToken cancellationToken = default)
    {
        var fb = Builders<TDocument>.Filter;
        var filter = fb.Lt(TimeFieldName, thresholdExclusive);
        var result = await Collection.DeleteManyAsync(filter, cancellationToken);
        return result.DeletedCount;
    }

    public async Task<long> DeleteOlderThanByMetaAsync(DateTime thresholdExclusive, object? meta,
        CancellationToken cancellationToken = default)
    {
        var fb = Builders<TDocument>.Filter;
        var filter = fb.And(fb.Lt(TimeFieldName, thresholdExclusive), MetaEquals(meta));
        var result = await Collection.DeleteManyAsync(filter, cancellationToken);
        return result.DeletedCount;
    }

    private static string? GetMemberPath(LambdaExpression expression)
    {
        return Recurse(expression.Body);

        static string BuildPath(MemberExpression me)
        {
            var parts = new Stack<string>();
            Expression? current = me;
            while (current is MemberExpression m)
            {
                parts.Push(m.Member.Name);
                current = m.Expression;
            }

            return string.Join(".", parts);
        }

        static string? Recurse(Expression expr)
        {
            return expr switch
            {
                MemberExpression me => BuildPath(me),
                UnaryExpression
                {
                    NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked, Operand: var op
                } => Recurse(op),
                _ => null
            };
        }
    }
}
