using FluentResults;

namespace LSL.Common.Models;

public class WarningReason(string message) : IWarning
{
    public string Message { get; } = message;

    public Dictionary<string, object> Metadata { get; } = new();
}