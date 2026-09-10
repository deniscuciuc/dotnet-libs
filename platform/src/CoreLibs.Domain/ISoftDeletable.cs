namespace CoreLibs.Domain;

/// <summary>
/// Marker interface for entities that support soft-delete.
/// A global EF query filter should exclude soft-deleted rows unless explicitly included.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
