using DenisCuciuc.Platform.MongoDB.Exceptions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Polly;

namespace DenisCuciuc.Platform.MongoDB.Actor;

/// <summary>
/// CAS-based orchestrator: locates an entity, creates an actor, applies mutations,
/// then persists with optimistic concurrency (compare-and-swap on the <c>_cas</c> field).
/// Retries automatically on <see cref="CasException"/> via Polly.
/// </summary>
public sealed class ActorOperations<TEntity, TActor, TActorResult>
    where TEntity : EntityCas
    where TActor : Actor<TEntity>, TActorResult
    where TActorResult : IActorResult<TEntity>
{
    private const int Retries = 10;
    private static readonly TimeSpan CacheTimeout = TimeSpan.FromSeconds(300);

    private readonly IServiceProvider _serviceProvider;
    private readonly ActorRepository<TEntity, TActor, TActorResult> _repository;
    private readonly IEntityCache<TEntity> _cache;
    private readonly IActorFactory<TEntity, TActor> _factory;
    private readonly IActorFinalizer<TEntity, TActorResult>? _finalizer;
    private readonly AsyncPolicy _retryPolicy;

    public ActorOperations(
        IServiceProvider serviceProvider,
        ActorRepository<TEntity, TActor, TActorResult> repository,
        IEntityCache<TEntity> cache,
        IActorFactory<TEntity, TActor> factory,
        IActorFinalizer<TEntity, TActorResult>? finalizer)
    {
        _serviceProvider = serviceProvider;
        _repository = repository;
        _cache = cache;
        _factory = factory;
        _finalizer = finalizer;
        _retryPolicy = Policy.Handle<CasException>().RetryAsync(Retries, OnCasRetry);
    }

    /// <summary>
    /// Creates a new <see cref="ActorOperations{TEntity,TActor,TActorResult}"/> that resolves
    /// scoped services from a different <see cref="IServiceProvider"/>.
    /// </summary>
    public ActorOperations<TEntity, TActor, TActorResult> Scope(IServiceProvider provider)
    {
        return new ActorOperations<TEntity, TActor, TActorResult>(provider, _repository, _cache, _factory, _finalizer);
    }

    // â”€â”€ Execute (throw on failure) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<TActorResult> ExecuteAsync(ObjectId id, Action<TActor> update)
    {
        return await _retryPolicy.ExecuteAsync(FindAndUpdateAsync(id, update));
    }

    public async Task<TActorResult> ExecuteAsync(FilterDefinition<TEntity> filter, Action<TActor> update)
    {
        return await _retryPolicy.ExecuteAsync(FindAndUpdateAsync(filter, update));
    }

    public async Task<TActorResult> ExecuteAsync(
        FilterDefinition<TEntity> filter,
        SortDefinition<TEntity> sort,
        Action<TActor> update)
    {
        return await _retryPolicy.ExecuteAsync(FindAndUpdateAsync(filter, sort, update));
    }

    // â”€â”€ ExecuteSafe (capture exceptions) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<OperationResult<TActorResult, TEntity>> ExecuteSafeAsync(
        ObjectId id, Action<TActor> update)
    {
        var result = await _retryPolicy.ExecuteAndCaptureAsync(FindAndUpdateAsync(id, update));
        return new OperationResult<TActorResult, TEntity>(result.Result, result.FinalException);
    }

    public async Task<OperationResult<TActorResult, TEntity>> ExecuteSafeAsync(
        FilterDefinition<TEntity> filter, Action<TActor> update)
    {
        var result = await _retryPolicy.ExecuteAndCaptureAsync(FindAndUpdateAsync(filter, update));
        return new OperationResult<TActorResult, TEntity>(result.Result, result.FinalException);
    }

    public async Task<OperationResult<TActorResult, TEntity>> ExecuteSafeAsync(
        FilterDefinition<TEntity> filter,
        SortDefinition<TEntity> sort,
        Action<TActor> update)
    {
        var result = await _retryPolicy.ExecuteAndCaptureAsync(FindAndUpdateAsync(filter, sort, update));
        return new OperationResult<TActorResult, TEntity>(result.Result, result.FinalException);
    }

    // â”€â”€ Public read-through cache lookup â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<TEntity> TryFindAsync(ObjectId id)
    {
        return await _cache.GetAsync(id)
               ?? await _repository.Primary.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    // â”€â”€ Private: find â†’ actor â†’ save â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private Func<Task<TActorResult>> FindAndUpdateAsync(ObjectId id, Action<TActor> update)
    {
        return async () => await UpdateAsync(await FindAsync(id), update);
    }

    private Func<Task<TActorResult>> FindAndUpdateAsync(
        FilterDefinition<TEntity> filter, Action<TActor> update)
    {
        return async () => await UpdateAsync(await FindAsync(filter), update);
    }

    private Func<Task<TActorResult>> FindAndUpdateAsync(
        FilterDefinition<TEntity> filter, SortDefinition<TEntity> sort, Action<TActor> update)
    {
        return async () => await UpdateAsync(await FindAsync(filter, sort), update);
    }

    private async Task<TEntity> FindAsync(ObjectId id)
    {
        return (await _cache.GetAsync(id)
                ?? await _repository.Primary.Find(x => x.Id == id).FirstOrDefaultAsync())
               ?? throw new EntityNotFoundException(typeof(TEntity));
    }

    private async Task<TEntity> FindAsync(FilterDefinition<TEntity> filter)
    {
        var id = await _repository.Primary.Find(filter).Project(x => x.Id).FirstOrDefaultAsync();
        return await FindAsync(id);
    }

    private async Task<TEntity> FindAsync(FilterDefinition<TEntity> filter, SortDefinition<TEntity> sort)
    {
        var id = await _repository.Primary.Find(filter).Project(x => x.Id).Sort(sort).FirstOrDefaultAsync();
        return await FindAsync(id);
    }

    private async Task<TActorResult> UpdateAsync(TEntity entity, Action<TActor>? update)
    {
        var owner = _serviceProvider.GetService(typeof(ActorOwner)) as ActorOwner;
        var actor = _factory.Create(entity, owner);

        actor.PreProcessing();
        update?.Invoke(actor);
        actor.PostProcessing();

        if (actor is ObservableActor<TEntity> observable)
        {
            var updates = observable.GetUpdates().ToList();
            if (updates.Count > 0)
                await CheckAndSaveAsync(entity, updates);
        }
        else
        {
            await CheckAndSaveAsync(entity);
        }

        if (_finalizer is not null && _finalizer.CanHandle(actor))
            await _finalizer.HandleAsync(actor);

        return actor;
    }

    // â”€â”€ Private: CAS persistence â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task CheckAndSaveAsync(TEntity entity)
    {
        var filter = GetCasFilter(entity);
        entity.Cas++;
        entity.ModifiedAt = DateTime.UtcNow;

        ReplaceOneResult result;

        try
        {
            result = await _repository.Primary.ReplaceOneAsync(filter, entity, new ReplaceOptions());
        }
        catch (MongoCommandException ex) when (ex.Code == 11000)
        {
            throw new EntityDuplicatedException(typeof(TEntity));
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new EntityDuplicatedException(typeof(TEntity));
        }

        if (result.MatchedCount != 1 && result.ModifiedCount != 1)
        {
            await _cache.RemoveAsync(entity.Id);
            throw new CasException(entity);
        }

        await _cache.SetAsync(entity.Id, entity, CacheTimeout);
    }

    private async Task CheckAndSaveAsync(TEntity entity, List<UpdateDefinition<TEntity>> updates)
    {
        var filter = GetCasFilter(entity);
        entity.Cas++;
        entity.ModifiedAt = DateTime.UtcNow;

        updates.Add(Builders<TEntity>.Update.Set(x => x.Cas, entity.Cas));
        updates.Add(Builders<TEntity>.Update.Set(x => x.ModifiedAt, entity.ModifiedAt));

        var combined = Builders<TEntity>.Update.Combine(updates);

        UpdateResult result;

        try
        {
            result = await _repository.Primary.UpdateOneAsync(filter, combined, new UpdateOptions());
        }
        catch (MongoCommandException ex) when (ex.Code == 11000)
        {
            throw new EntityDuplicatedException(typeof(TEntity));
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new EntityDuplicatedException(typeof(TEntity));
        }

        if (result.MatchedCount != 1 && result.ModifiedCount != 1)
        {
            await _cache.RemoveAsync(entity.Id);
            throw new CasException(entity);
        }

        await _cache.SetAsync(entity.Id, entity, CacheTimeout);
    }

    private static FilterDefinition<TEntity> GetCasFilter(TEntity entity)
    {
        return Builders<TEntity>.Filter.And(
            Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id),
            Builders<TEntity>.Filter.Eq(x => x.Cas, entity.Cas));
    }

    private void OnCasRetry(Exception exception, int retry)
    {
        if (exception is CasException ex)
            _repository.Logger.LogWarning(
                "Repository {EntityType} concurrent check-and-save (EntityId={EntityId}, EntityCas={EntityCas})",
                typeof(TEntity).Name, ex.Entity.Id, ex.Entity.Cas);
        else
            _repository.Logger.LogError(exception,
                "Repository {EntityType} check-and-save retry on unknown exception",
                typeof(TEntity).Name);
    }
}
