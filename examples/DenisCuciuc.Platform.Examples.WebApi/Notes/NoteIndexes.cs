using DenisCuciuc.Platform.MongoDB;

namespace DenisCuciuc.Platform.Examples.WebApi.Notes;

public sealed class NoteIndexes : IndexesBuilder<NoteEntity>
{
    public NoteIndexes()
    {
        Index(Ascending(n => n.Title)).Unique();
        Index(Descending(n => n.CreatedAt));
    }
}
