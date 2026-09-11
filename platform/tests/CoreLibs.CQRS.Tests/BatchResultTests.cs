using ErrorOr;

namespace CoreLibs.CQRS.Tests;

public class BatchResultTests
{
    [Fact]
    public void SuccessCount_CountsSuccessful()
    {
        var results = new BatchResult([
            new(Guid.NewGuid(), true),
            new(Guid.NewGuid(), false, Error: ErrorOr.Error.Failure("err", "fail")),
            new(Guid.NewGuid(), true)
        ]);

        Assert.Equal(2, results.SuccessCount);
        Assert.Equal(1, results.FailureCount);
        Assert.False(results.AllSucceeded);
    }

    [Fact]
    public void AllSucceeded_True_WhenAllSuccess()
    {
        var results = new BatchResult([
            new(Guid.NewGuid(), true),
            new(Guid.NewGuid(), true)
        ]);

        Assert.True(results.AllSucceeded);
    }
}
