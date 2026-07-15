using MediatR;
using Order.Api.Shared;

namespace Order.Api.Features.PlaceOrder;

public class PlaceOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/{id}/place", async (string id, IMediator mediator) =>
        {
            var result = await mediator.Send(new PlaceOrderCommand(id));
            return result.ToHttpResult();
        });
    }
}
