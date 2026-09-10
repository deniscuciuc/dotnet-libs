namespace CoreLibs.Examples.WebApi.Notes;

public sealed record NoteResponse(string Id, string Title, string Content, DateTime CreatedAt, DateTime ModifiedAt);
