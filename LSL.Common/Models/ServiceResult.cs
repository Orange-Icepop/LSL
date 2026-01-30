using System.Diagnostics.CodeAnalysis;

namespace LSL.Common.Models;

// 表示无返回值的单位类型
public readonly record struct Unit
{
    public static readonly Unit Value = default;
    public override string ToString() => "()";
}

public enum ServiceResultType
{
    Success,
    Warning,
    Error,
}

public record ServiceResult<T>
{
    public T? Result { get; }
    public ServiceResultType ResultType { get; }
    public Exception? Error { get; }
    
    [MemberNotNullWhen(true, nameof(Result))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => ResultType == ServiceResultType.Success;
    [MemberNotNullWhen(true, nameof(Result))]
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsWarning => ResultType == ServiceResultType.Warning;
    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Result))]
    public bool IsError => ResultType == ServiceResultType.Error;
    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Result))]
    public bool HasError => ResultType != ServiceResultType.Success;
    [MemberNotNullWhen(true, nameof(Result))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool HasResult => ResultType != ServiceResultType.Error;

    // 内部构造器防止不规范创建
    internal ServiceResult(ServiceResultType errorCode, T? result, Exception? error)
    {
        ResultType = errorCode;
        Result = result;
        Error = error;
    }
    public void Unwrap()
    {
        if (HasError) throw Error;
    }
}

public record ServiceResult : ServiceResult<Unit>
{
    private ServiceResult(ServiceResultType ResultType, Exception? Error) : base(ResultType, Unit.Value, Error){}

    public static ServiceResult Success() => new (ServiceResultType.Success, null);
    public static ServiceResult Fail(Exception error) => new(ServiceResultType.Error, error);
    public static ServiceResult Fail(string error) => new(ServiceResultType.Error, new Exception(error));
    public static ServiceResult Warning(Exception error) => new(ServiceResultType.Warning, error);
    public static ServiceResult Warning(string error) => new(ServiceResultType.Warning, new Exception(error));
    
    public static ServiceResult<T> Success<T>(T result) => new (ServiceResultType.Success, result, null);
    public static ServiceResult<T> Fail<T>(Exception error) => new(ServiceResultType.Error, default, error);
    public static ServiceResult<T> Fail<T>(string error) => new(ServiceResultType.Error, default, new Exception(error));
    public static ServiceResult<T> Fail<T>(T? fallbackResult, Exception error) => new(ServiceResultType.Error, fallbackResult, error);
    public static ServiceResult<T> Warning<T>(T result, Exception error) => new(ServiceResultType.Warning, result, error);
    public static ServiceResult<T> Warning<T>(T result, string error) => new(ServiceResultType.Warning, result,
        new Exception(error));
}
