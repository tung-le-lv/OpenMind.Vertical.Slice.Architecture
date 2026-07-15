using MediatR;
using Order.Api.Domain.Enums;
using Order.Api.Shared;

namespace Order.Api.Features.UpdateOrderStatus;

public record UpdateOrderStatusCommand(
    string OrderId,
    OrderStatus NewStatus
) : IRequest<UpdateOrderStatusResult>;

public record UpdateOrderStatusResult(
    bool Success,
    string? Message,
    List<string>? Errors
) : IOperationResult;
