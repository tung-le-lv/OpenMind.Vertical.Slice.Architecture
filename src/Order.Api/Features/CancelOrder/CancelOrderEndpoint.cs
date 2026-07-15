using MediatR;
using Order.Api.Shared;

namespace Order.Api.Features.CancelOrder;

public class CancelOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/{id}/cancel", async (string id, IMediator mediator) =>
        {
            var result = await mediator.Send(new CancelOrderCommand(id));
            return result.ToHttpResult();
        });
    }
}
