namespace Order.Api.Shared;

public static class ApiResults
{
    public static IResult ToHttpResult(this IOperationResult result, string successMessage = "OK")
    {
        if (result.Success)
        {
            return Results.Ok(ApiResponse<string>.SuccessResponse(successMessage, result.Message));
        }

        var statusCode = result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;

        return Results.Json(ApiResponse<string>.ErrorResponse(result.Message ?? "Request failed.", result.Errors), statusCode: statusCode);
    }
}
