using MediatR;
using Order.Api.Domain.Enums;
using Order.Api.Shared;

namespace Order.Api.Features.UpdateOrderStatus;

public class UpdateOrderStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/orders/{id}/status", async (string id, UpdateStatusRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateOrderStatusCommand(id, request.Status));
            return result.ToHttpResult();
        });
    }

    public record UpdateStatusRequest(OrderStatus Status);
}
