using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace DenisCuciuc.Platform.CQRS;

/// <summary>
/// MediatR pipeline behavior that runs FluentValidation validators before the handler.
/// If any validation errors are found, an <see cref="ErrorOr{T}"/> with validation errors is returned
/// without executing the handler.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResult>(
    IEnumerable<IValidator<TRequest>> validators,
    IOptionsMonitor<PlatformCqrsOptions> optionsMonitor)
    : IPipelineBehavior<TRequest, ErrorOr<TResult>>
    where TRequest : IRequest<ErrorOr<TResult>>
{
    public async Task<ErrorOr<TResult>> Handle(
        TRequest request,
        RequestHandlerDelegate<ErrorOr<TResult>> next,
        CancellationToken cancellationToken)
    {
        var options = optionsMonitor.CurrentValue;
        if (!options.EnableValidationBehavior)
            return await next();

        var requestValidators = validators as IValidator<TRequest>[] ?? validators.ToArray();
        if (requestValidators.Length == 0)
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            requestValidators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .DistinctBy(x => (x.Code, x.Description, x.Type))
            .ToList();

        if (errors.Count != 0)
        {
            return errors;
        }

        return await next();
    }
}
