using MediatR;
using Order.Api.Shared;
using Order.Api.Shared.Application.Dtos;

namespace Order.Api.Features.GetOrder;

public class GetOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{id}", async (string id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetOrderQuery(id));

            return result is null
                ? Results.NotFound(ApiResponse<OrderDto>.ErrorResponse($"Order with ID '{id}' not found."))
                : Results.Ok(ApiResponse<OrderDto>.SuccessResponse(result));
        });
    }
}
