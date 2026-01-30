using System.Diagnostics.CodeAnalysis;

namespace LSL.Common;

// 表示无返回值的单位类型
public readonly record struct Unit
{
    public static readonly Unit Value = default;
    public override string ToString() => "()";
}

public enum ResultType
{
    Success,
    Warning,
    Error,
}

public record ServiceResult<T>
{
    public T? Value { get; }
    public ResultType Kind { get; }
    public Exception? Error { get; }
    
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Kind is ResultType.Success;
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsWarning => Kind is ResultType.Warning;
    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsError => Kind is ResultType.Error;
    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool HasError => Kind is not ResultType.Success;
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool HasResult => Kind is not ResultType.Error;

    // 内部构造器防止不规范创建
    internal ServiceResult(ResultType errorCode, T? value, Exception? error)
    {
        Kind = errorCode;
        Value = value;
        Error = error;
    }
}

public record ServiceResult : ServiceResult<Unit>
{
    private ServiceResult(ResultType kind, Exception? Error) : base(kind, Unit.Value, Error){}

    public static ServiceResult Success() => new (ResultType.Success, null);
    public static ServiceResult Fail(Exception error) => new(ResultType.Error, error);
    public static ServiceResult Fail(string error) => new(ResultType.Error, new Exception(error));
    public static ServiceResult Warning(Exception error) => new(ResultType.Warning, error);
    public static ServiceResult Warning(string error) => new(ResultType.Warning, new Exception(error));
    
    public static ServiceResult<T> Success<T>(T result) => new (ResultType.Success, result, null);
    public static ServiceResult<T> Fail<T>(Exception error) => new(ResultType.Error, default, error);
    public static ServiceResult<T> Fail<T>(string error) => new(ResultType.Error, default, new Exception(error));
    public static ServiceResult<T> Fail<T>(T? fallbackResult, Exception error) => new(ResultType.Error, fallbackResult, error);
    public static ServiceResult<T> Warning<T>(T result, Exception error) => new(ResultType.Warning, result, error);
    public static ServiceResult<T> Warning<T>(T result, string error) => new(ResultType.Warning, result,
        new Exception(error));
}
