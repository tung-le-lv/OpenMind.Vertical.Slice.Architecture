using MediatR;
using Order.Api.Shared;
using Order.Api.Shared.Application.Dtos;

namespace Order.Api.Features.GetOrdersByDateRange;

public class GetOrdersByDateRangeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/filter", async (string? date, IMediator mediator) =>
        {
            if (!DateOnly.TryParse(date, out var parsedDate))
            {
                return Results.BadRequest(ApiResponse<IEnumerable<OrderDto>>.ErrorResponse("Query parameter 'date' must be a valid date (YYYY-MM-DD)."));
            }

            var result = await mediator.Send(new GetOrdersByDateRangeQuery(parsedDate));
            return Results.Ok(ApiResponse<IEnumerable<OrderDto>>.SuccessResponse(result));
        });
    }
}
