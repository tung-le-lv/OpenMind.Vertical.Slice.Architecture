using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Order.Api.Shared;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors.Select(e => e.ErrorMessage).ToList();
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse<string>.ErrorResponse("Validation failed.", errors), cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception processing {Path}", httpContext.Request.Path);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(ApiResponse<string>.ErrorResponse("Internal server error.", [exception.Message]), cancellationToken);
        return true;
    }
}
