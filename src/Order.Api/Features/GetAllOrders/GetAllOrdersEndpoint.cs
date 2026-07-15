using MediatR;
using Order.Api.Shared;
using Order.Api.Shared.Application.Dtos;

namespace Order.Api.Features.GetAllOrders;

public class GetAllOrdersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetAllOrdersQuery());
            return Results.Ok(ApiResponse<IEnumerable<OrderDto>>.SuccessResponse(result));
        });
    }
}
