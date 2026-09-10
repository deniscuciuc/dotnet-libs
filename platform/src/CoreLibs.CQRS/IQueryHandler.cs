using ErrorOr;
using MediatR;

namespace CoreLibs.CQRS;

/// <summary>
/// Handler for queries that return a value of type <typeparamref name="TResponse"/>.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, ErrorOr<TResponse>>
    where TQuery : IQuery<TResponse>;
