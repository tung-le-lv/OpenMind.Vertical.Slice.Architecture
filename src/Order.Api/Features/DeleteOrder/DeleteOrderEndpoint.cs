using MediatR;
using Order.Api.Shared;

namespace Order.Api.Features.DeleteOrder;

public class DeleteOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/orders/{id}", async (string id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteOrderCommand(id));

            return result.Success
                ? Results.Ok(ApiResponse<string>.SuccessResponse("OK", result.Message))
                : Results.BadRequest(ApiResponse<string>.ErrorResponse(result.Message ?? "Failed to delete order."));
        });
    }
}
