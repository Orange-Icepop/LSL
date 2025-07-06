namespace LSL.Common.Core;

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