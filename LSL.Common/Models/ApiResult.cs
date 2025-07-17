namespace LSL.Common.Models;

public class ApiResult
{
    public int StatusCode { get; }
    public string Message { get; }

    public ApiResult(int statusCode, string? msg = null)
    {
        StatusCode = statusCode;
        Message = msg ?? string.Empty;
    }
}