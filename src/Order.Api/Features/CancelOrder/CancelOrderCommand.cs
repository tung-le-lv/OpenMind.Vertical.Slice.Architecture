using MediatR;
using Order.Api.Shared;

namespace Order.Api.Features.CancelOrder;

public record CancelOrderCommand(string OrderId) : IRequest<CancelOrderResult>;

public record CancelOrderResult(
    bool Success,
    string? Message,
    List<string>? Errors
) : IOperationResult;
