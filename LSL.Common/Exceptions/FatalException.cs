namespace LSL.Common.Exceptions;

public class FatalException : LSLException
{
    public FatalException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public FatalException(string message) : base(message)
    {
    }

    public FatalException()
    {
    }

} // 致命错误
