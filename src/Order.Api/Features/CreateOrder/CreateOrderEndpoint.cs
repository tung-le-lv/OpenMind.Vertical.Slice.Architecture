using MediatR;
using Order.Api.Shared;

namespace Order.Api.Features.CreateOrder;

public class CreateOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (CreateOrderCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            if (!result.Success)
            {
                return Results.BadRequest(ApiResponse<string>.ErrorResponse(result.Message ?? "Failed to create order.", result.Errors));
            }

            return Results.Created($"/orders/{result.OrderId}", ApiResponse<object>.SuccessResponse(new { result.OrderId }, result.Message));
        });
    }
}
