namespace LSL.Common.Exceptions;

public class NonfatalException : LSLException
{
    public NonfatalException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public NonfatalException(string message) : base(message)
    {
    }

    public NonfatalException()
    {
    }

} // 非致命错误