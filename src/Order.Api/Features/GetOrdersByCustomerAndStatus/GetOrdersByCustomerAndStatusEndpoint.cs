using MediatR;
using Order.Api.Domain.Enums;
using Order.Api.Shared;
using Order.Api.Shared.Application.Dtos;

namespace Order.Api.Features.GetOrdersByCustomerAndStatus;

public class GetOrdersByCustomerAndStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/customer/{customerId}/status/{status}", async (string customerId, string status, IMediator mediator) =>
        {
            if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                return Results.BadRequest(ApiResponse<IEnumerable<OrderDto>>.ErrorResponse(
                    $"Invalid status '{status}'. Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}"));
            }

            var result = await mediator.Send(new GetOrdersByCustomerAndStatusQuery(customerId, parsedStatus));
            return Results.Ok(ApiResponse<IEnumerable<OrderDto>>.SuccessResponse(result));
        });
    }
}
