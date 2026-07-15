using MediatR;
using Order.Api.Shared;

namespace Order.Api.Features.AddOrderItem;

public class AddOrderItemEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/{id}/items", async (string id, AddItemRequest request, IMediator mediator) =>
        {
            var command = new AddOrderItemCommand(id, request.ProductId, request.ProductName, request.Quantity, request.UnitPrice);
            var result = await mediator.Send(command);
            return result.ToHttpResult();
        });
    }

    public record AddItemRequest(string ProductId, string ProductName, int Quantity, decimal UnitPrice);
}
