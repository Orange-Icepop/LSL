using System.Diagnostics.CodeAnalysis;

namespace LSL.Common.Models;

public interface IServiceResult
{
    ServiceResultType ResultType { get; }
    Exception? Error { get; }
    
    [MemberNotNullWhen(false, nameof(Error))]
    bool IsSuccess => ResultType == ServiceResultType.Success;
    [MemberNotNullWhen(true, nameof(Error))]
    bool IsFinishedWithWarning => ResultType == ServiceResultType.FinishWithWarning;
    [MemberNotNullWhen(true, nameof(Error))]
    bool IsError => ResultType == ServiceResultType.Error;
    [MemberNotNullWhen(true, nameof(Error))]
    bool HasError => ResultType != ServiceResultType.Success;
}

public record ServiceResult<T> : IServiceResult
{
    public T? Result { get; }
    public ServiceResultType ResultType { get; }
    public Exception? Error { get; }
    
    [MemberNotNullWhen(true, nameof(Result))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => ResultType == ServiceResultType.Success;
    [MemberNotNullWhen(true, nameof(Result))]
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFinishedWithWarning => ResultType == ServiceResultType.FinishWithWarning;
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => ResultType == ServiceResultType.Error;
    [MemberNotNullWhen(true, nameof(Error))]
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
}

public record ServiceResult : IServiceResult
{
    private ServiceResult(ServiceResultType ResultType, Exception? Error)
    {
        this.ResultType = ResultType;
        this.Error = Error;
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => ResultType == ServiceResultType.Success;
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFinishedWithWarning => ResultType == ServiceResultType.FinishWithWarning;
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => ResultType == ServiceResultType.Error;
    [MemberNotNullWhen(true, nameof(Error))]
    public bool HasError => ResultType != ServiceResultType.Success;

    public ServiceResultType ResultType { get; init; }
    public Exception? Error { get; init; }


    public static ServiceResult Success() => new (ServiceResultType.Success, null);
    public static ServiceResult Fail(Exception error) => new(ServiceResultType.Error, error);
    public static ServiceResult FinishWithWarning(Exception error) => new(ServiceResultType.FinishWithWarning, error);
    
    public static ServiceResult<T> Success<T>(T result) => new (ServiceResultType.Success, result, null);
    public static ServiceResult<T> Fail<T>(Exception error) => new(ServiceResultType.Error, default, error);
    public static ServiceResult<T> Fail<T>(T? fallbackResult, Exception error) => new(ServiceResultType.Error, fallbackResult, error);
    public static ServiceResult<T> FinishWithWarning<T>(T result, Exception error) => new(ServiceResultType.FinishWithWarning, result, error);
}

public enum ServiceResultType
{
    Success,
    Error,
    FinishWithWarning,
}