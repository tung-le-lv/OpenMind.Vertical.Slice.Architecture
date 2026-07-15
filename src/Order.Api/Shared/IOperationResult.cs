namespace Order.Api.Shared;

public interface IOperationResult
{
    bool Success { get; }

    string? Message { get; }

    List<string>? Errors { get; }
}
