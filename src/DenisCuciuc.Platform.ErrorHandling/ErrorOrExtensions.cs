using ErrorOr;
using Microsoft.AspNetCore.Http;

namespace DenisCuciuc.Platform.ErrorHandling;

public static class ErrorOrExtensions
{
    public static IResult ToProblemResult<T>(this ErrorOr<T> errorOr)
    {
        return !errorOr.IsError
            ? throw new InvalidOperationException("Cannot convert a successful result to a problem result.")
            : errorOr.Errors.ToProblemResult();
    }

    public static IResult ToProblemResult(this List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
        }

        return errors.All(e => e.Type == ErrorType.Validation)
            ? CreateValidationProblem(errors)
            : CreateProblem(errors[0]);
    }

    public static IResult Match<T>(this ErrorOr<T> errorOr, Func<T, IResult> onSuccess)
    {
        return errorOr.IsError
            ? errorOr.ToProblemResult()
            : onSuccess(errorOr.Value);
    }

    public static async Task<IResult> MatchAsync<T>(this Task<ErrorOr<T>> errorOrTask, Func<T, IResult> onSuccess)
    {
        var errorOr = await errorOrTask;
        return errorOr.IsError
            ? errorOr.ToProblemResult()
            : onSuccess(errorOr.Value);
    }

    private static IResult CreateProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            statusCode: statusCode,
            title: GetTitle(error.Type),
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = error.Code
            });
    }

    private static IResult CreateValidationProblem(List<Error> errors)
    {
        var errorDictionary = errors
            .GroupBy(e => e.Code)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray());

        return Results.ValidationProblem(errorDictionary);
    }

    private static string GetTitle(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => "Bad Request",
            ErrorType.NotFound => "Not Found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            _ => "Internal Server Error"
        };
    }
}
