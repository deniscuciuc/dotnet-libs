using ErrorOr;
using MediatR;

namespace CoreLibs.CQRS;

/// <summary>
/// Marker interface for commands that return no value.
/// </summary>
public interface ICommand : IRequest<ErrorOr<Unit>>;

/// <summary>
/// Marker interface for commands that return a value of type <typeparamref name="TResponse"/>.
/// </summary>
public interface ICommand<TResponse> : IRequest<ErrorOr<TResponse>>;
