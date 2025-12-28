namespace LSL.Common.Exceptions;

public class ServerConfigUnparsableException : Exception
{
    public ServerConfigUnparsableException(string msg) : base(msg)
    {
    }

    public ServerConfigUnparsableException(string msg, Exception innerException) : base(msg, innerException)
    {
        
    }
}