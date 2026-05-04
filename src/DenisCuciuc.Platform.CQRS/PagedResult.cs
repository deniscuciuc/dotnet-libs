namespace DenisCuciuc.Platform.CQRS;

public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int TotalCount { get; init; }

    public required int PageNumber { get; init; }

    public required int PageSize { get; init; }

    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNext => PageNumber < TotalPages;

    public bool HasPrevious => PageNumber > 1;

    public bool IsEmpty => Items.Count == 0;

    public int FirstItemIndex => IsEmpty ? 0 : (PageNumber - 1) * PageSize + 1;

    public int LastItemIndex => IsEmpty ? 0 : Math.Min(PageNumber * PageSize, TotalCount);

    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
