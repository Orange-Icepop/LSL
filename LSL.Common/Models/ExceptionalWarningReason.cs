using FluentResults;

namespace LSL.Common.Models;

public class ExceptionalWarningReason(string message, Exception exception) : WarningReason(message)
{
    public Exception Exception { get; } = exception;
    public ExceptionalWarningReason(Exception exception) : this(exception.Message, exception) { }
}