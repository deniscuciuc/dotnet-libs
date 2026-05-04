namespace DenisCuciuc.Platform.CQRS.Tests;

public class PagedResultTests
{
    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var items = new[] { "a", "b", "c" };
        var result = PagedResult<string>.Create(items, totalCount: 10, pageNumber: 1, pageSize: 3);

        Assert.Equal(items, result.Items);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(3, result.PageSize);
    }

    [Fact]
    public void TotalPages_CalculatesCorrectly()
    {
        var result = PagedResult<int>.Create([], totalCount: 10, pageNumber: 1, pageSize: 3);

        Assert.Equal(4, result.TotalPages); // ceil(10/3) = 4
    }

    [Fact]
    public void TotalPages_ExactDivision()
    {
        var result = PagedResult<int>.Create([], totalCount: 9, pageNumber: 1, pageSize: 3);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void HasNext_True_WhenNotOnLastPage()
    {
        var result = PagedResult<int>.Create([1, 2, 3], totalCount: 10, pageNumber: 1, pageSize: 3);

        Assert.True(result.HasNext);
    }

    [Fact]
    public void HasNext_False_WhenOnLastPage()
    {
        var result = PagedResult<int>.Create([10], totalCount: 10, pageNumber: 4, pageSize: 3);

        Assert.False(result.HasNext);
    }

    [Fact]
    public void HasPrevious_False_OnFirstPage()
    {
        var result = PagedResult<int>.Create([1], totalCount: 5, pageNumber: 1, pageSize: 3);

        Assert.False(result.HasPrevious);
    }

    [Fact]
    public void HasPrevious_True_OnSecondPage()
    {
        var result = PagedResult<int>.Create([4, 5], totalCount: 5, pageNumber: 2, pageSize: 3);

        Assert.True(result.HasPrevious);
    }

    [Fact]
    public void IsEmpty_True_WhenNoItems()
    {
        var result = PagedResult<int>.Create([], totalCount: 0, pageNumber: 1, pageSize: 10);

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void IsEmpty_False_WhenHasItems()
    {
        var result = PagedResult<int>.Create([1], totalCount: 1, pageNumber: 1, pageSize: 10);

        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Create_ThrowsOnInvalidPageNumber()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PagedResult<int>.Create([], totalCount: 0, pageNumber: 0, pageSize: 10));
    }

    [Fact]
    public void Create_ThrowsOnInvalidPageSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PagedResult<int>.Create([], totalCount: 0, pageNumber: 1, pageSize: 0));
    }

    [Fact]
    public void Create_ThrowsOnNegativeTotalCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PagedResult<int>.Create([], totalCount: -1, pageNumber: 1, pageSize: 10));
    }

    [Fact]
    public void FirstItemIndex_CalculatesCorrectly()
    {
        var result = PagedResult<int>.Create([4, 5, 6], totalCount: 10, pageNumber: 2, pageSize: 3);

        Assert.Equal(4, result.FirstItemIndex); // (2-1)*3+1 = 4
    }

    [Fact]
    public void LastItemIndex_CalculatesCorrectly()
    {
        var result = PagedResult<int>.Create([4, 5, 6], totalCount: 10, pageNumber: 2, pageSize: 3);

        Assert.Equal(6, result.LastItemIndex); // min(2*3, 10) = 6
    }

    [Fact]
    public void FirstItemIndex_Empty_ReturnsZero()
    {
        var result = PagedResult<int>.Create([], totalCount: 0, pageNumber: 1, pageSize: 10);

        Assert.Equal(0, result.FirstItemIndex);
    }

    [Fact]
    public void LastItemIndex_LastPage_ClampedToTotalCount()
    {
        var result = PagedResult<int>.Create([10], totalCount: 10, pageNumber: 4, pageSize: 3);

        Assert.Equal(10, result.LastItemIndex); // min(4*3=12, 10) = 10
    }
}
