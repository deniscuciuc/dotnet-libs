using ErrorOr;
using MediatR;

namespace CoreLibs.CQRS;

/// <summary>
/// Handler for commands that return no value.
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, ErrorOr<Unit>>
    where TCommand : ICommand;

/// <summary>
/// Handler for commands that return a value of type <typeparamref name="TResponse"/>.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, ErrorOr<TResponse>>
    where TCommand : ICommand<TResponse>;
