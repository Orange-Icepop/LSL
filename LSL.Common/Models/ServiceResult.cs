using System.Diagnostics.CodeAnalysis;

namespace LSL.Common.Models;

public interface IServiceResult
{
    ServiceResultType ResultType { get; }
    Exception? Error { get; }
    
    [MemberNotNullWhen(false, nameof(Error))]
    bool IsSuccess => ResultType == ServiceResultType.Success;
    [MemberNotNullWhen(true, nameof(Error))]
    bool IsWarning => ResultType == ServiceResultType.Warning;
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
    public ServiceResult<TContinue> Then<TContinue>(Func<T, ServiceResult<TContinue>> continuation)
    {
        if (this.IsError) 
            return ServiceResult.Fail<TContinue>(Error);
    
        if (this.IsWarning)
        {
            var nextResult = continuation(Result);
            
            if (nextResult.IsSuccess) return ServiceResult.Warning(nextResult.Result, Error);
            if (nextResult.IsWarning) return ServiceResult.Warning(nextResult.Result, new AggregateException(Error, nextResult.Error));
            return ServiceResult.Fail<TContinue>(new AggregateException(Error, nextResult.Error));
        }
        
        return continuation(Result);
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
    public bool IsWarning => ResultType == ServiceResultType.Warning;
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => ResultType == ServiceResultType.Error;
    [MemberNotNullWhen(true, nameof(Error))]
    public bool HasError => ResultType != ServiceResultType.Success;

    public ServiceResultType ResultType { get; init; }
    public Exception? Error { get; init; }


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

public enum ServiceResultType
{
    Success,
    Warning,
    Error,
}