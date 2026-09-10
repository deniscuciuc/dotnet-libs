using CoreLibs.MongoDB;

namespace CoreLibs.Examples.WebApi.Notes;

public sealed class NoteIndexes : IndexesBuilder<NoteEntity>
{
    public NoteIndexes()
    {
        Index(Ascending(n => n.Title)).Unique();
        Index(Descending(n => n.CreatedAt));
    }
}
