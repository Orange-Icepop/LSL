namespace LSL.Common.Exceptions;

public abstract class LSLException : Exception // LSL异常，Exception的包装类
{
    protected LSLException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected LSLException(string message) : base(message)
    {
    }

    protected LSLException()
    {
    }
}