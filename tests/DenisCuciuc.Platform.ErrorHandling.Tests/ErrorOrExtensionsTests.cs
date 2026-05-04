using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DenisCuciuc.Platform.ErrorHandling.Tests;

public class ErrorOrExtensionsTests
{
    [Fact]
    public void Match_Success_CallsOnSuccess()
    {
        ErrorOr<string> result = "hello";

        var httpResult = result.Match(v => Results.Ok(v));

        Assert.IsType<Ok<string>>(httpResult);
    }

    [Fact]
    public void Match_Error_ReturnsProblem()
    {
        ErrorOr<string> result = Error.NotFound("Item.NotFound", "Item was not found");

        var httpResult = result.Match(v => Results.Ok(v));

        Assert.IsType<ProblemHttpResult>(httpResult);
    }

    [Fact]
    public async Task MatchAsync_Success_CallsOnSuccess()
    {
        var task = Task.FromResult<ErrorOr<int>>(42);

        var httpResult = await task.MatchAsync(v => Results.Ok(v));

        Assert.IsType<Ok<int>>(httpResult);
    }

    [Fact]
    public async Task MatchAsync_Error_ReturnsProblem()
    {
        var task = Task.FromResult<ErrorOr<int>>(Error.Forbidden("Auth.Denied", "Access denied"));

        var httpResult = await task.MatchAsync(v => Results.Ok(v));

        Assert.IsType<ProblemHttpResult>(httpResult);
    }

    [Fact]
    public void ToProblemResult_NotFoundError_Returns404()
    {
        ErrorOr<string> result = Error.NotFound("Resource.NotFound", "Not found");

        var httpResult = result.ToProblemResult();

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public void ToProblemResult_ConflictError_Returns409()
    {
        ErrorOr<string> result = Error.Conflict("Resource.Conflict", "Conflict");

        var httpResult = result.ToProblemResult();

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public void ToProblemResult_UnauthorizedError_Returns401()
    {
        ErrorOr<string> result = Error.Unauthorized("Auth.Unauthorized", "Unauthorized");

        var httpResult = result.ToProblemResult();

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.StatusCode);
    }

    [Fact]
    public void ToProblemResult_ForbiddenError_Returns403()
    {
        ErrorOr<string> result = Error.Forbidden("Auth.Forbidden", "Forbidden");

        var httpResult = result.ToProblemResult();

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status403Forbidden, problem.StatusCode);
    }

    [Fact]
    public void ToProblemResult_ValidationErrors_ReturnsValidationProblem()
    {
        var errors = new List<Error>
        {
            Error.Validation("Name", "Name is required"),
            Error.Validation("Email", "Email is invalid")
        };

        var httpResult = errors.ToProblemResult();

        // Results.ValidationProblem returns ProblemHttpResult in .NET 10
        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public void ToProblemResult_EmptyErrors_Returns500()
    {
        var errors = new List<Error>();

        var httpResult = errors.ToProblemResult();

        var problem = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
    }

    [Fact]
    public void ToProblemResult_SuccessfulResult_Throws()
    {
        ErrorOr<string> result = "success";

        Assert.Throws<InvalidOperationException>(() => result.ToProblemResult());
    }
}
