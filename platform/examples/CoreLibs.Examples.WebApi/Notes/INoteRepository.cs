using CoreLibs.MongoDB;

namespace CoreLibs.Examples.WebApi.Notes;

public interface INoteRepository : IRepository<NoteEntity>
{
    Task<NoteEntity?> GetByIdAsync(string id);
    Task<(List<NoteEntity> Items, long TotalCount)> GetPagedAsync(int page, int pageSize);
    Task InsertAsync(NoteEntity entity);
    Task<bool> UpdateAsync(string id, string title, string content);
    Task<bool> DeleteAsync(string id);
}
