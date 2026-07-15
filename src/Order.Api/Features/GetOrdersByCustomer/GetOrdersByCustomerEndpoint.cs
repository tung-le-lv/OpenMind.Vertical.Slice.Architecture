using MediatR;
using Order.Api.Shared;
using Order.Api.Shared.Application.Dtos;

namespace Order.Api.Features.GetOrdersByCustomer;

public class GetOrdersByCustomerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/customer/{customerId}", async (string customerId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetOrdersByCustomerQuery(customerId));
            return Results.Ok(ApiResponse<IEnumerable<OrderDto>>.SuccessResponse(result));
        });
    }
}
