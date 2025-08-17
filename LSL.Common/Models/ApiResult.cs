namespace LSL.Common.Models;

public record ApiResult(int StatusCode, string Message = "")
{
    public void Deconstruct(out int statusCode, out string message)
    {
        statusCode = StatusCode;
        message = Message;
    }
}