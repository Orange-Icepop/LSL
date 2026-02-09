namespace LSL.Common.Results;

public interface IResult
{
    ResultType Kind { get; }
    Exception? Error { get; }
}

public interface IResult<out T> : IResult
{
    T? Value { get; }
}