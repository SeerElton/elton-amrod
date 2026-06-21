namespace OrderManagement.Contracts.Responses;

public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiErrorResponse() { }

    public ApiErrorResponse(string message)
    {
        Message = message;
    }

    public ApiErrorResponse(string message, Dictionary<string, string[]>? errors)
    {
        Message = message;
        Errors = errors;
    }
}
