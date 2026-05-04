using DenisCuciuc.Platform.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DenisCuciuc.Platform.Examples.WebApi.Notes;

public sealed class NoteRepository(IMongoDBProvider provider, NoteIndexes indexes, ILogger<NoteRepository> logger)
    : EntityRepository<NoteEntity>(provider, indexes, logger), INoteRepository
{
    public async Task<NoteEntity?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        return await Find(Filter.Eq(n => n.Id, objectId))
            .FirstOrDefaultAsync();
    }

    public async Task<(List<NoteEntity> Items, long TotalCount)> GetPagedAsync(int page, int pageSize)
    {
        var filter = Filter.Empty;

        var totalCount = await Secondary.CountDocumentsAsync(filter);

        var items = await Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task InsertAsync(NoteEntity entity)
    {
        await Primary.InsertOneAsync(entity);
    }

    public async Task<bool> UpdateAsync(string id, string title, string content)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return false;

        var result = await Primary.UpdateOneAsync(
            Filter.Eq(n => n.Id, objectId),
            Update.Set(n => n.Title, title)
                .Set(n => n.Content, content)
                .Set(n => n.ModifiedAt, DateTime.UtcNow));

        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return false;

        var result = await Primary.DeleteOneAsync(Filter.Eq(n => n.Id, objectId));
        return result.DeletedCount > 0;
    }
}
