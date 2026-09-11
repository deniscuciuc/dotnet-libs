using ErrorOr;
using MediatR;

namespace CoreLibs.CQRS;

/// <summary>
/// Marker interface for queries that return a value of type <typeparamref name="TResponse"/>.
/// </summary>
public interface IQuery<TResponse> : IRequest<ErrorOr<TResponse>>;
