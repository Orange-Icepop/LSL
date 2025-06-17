using System;

namespace LSL.Common.Contracts;

public interface IServiceResult
{
    ServiceResultType ErrorCode { get; }
    Exception? Error { get; }
}

/*
 * ServiceError使用说明
 * ErrorCode为0时表示没有错误。
 * 1:执行完成，但是包含警告。
 * 2:出现错误。只代表最基础的错误返回。
 * -1:在没有执行完成的情况下中断了。
 */
public class ServiceResult<T>(ServiceResultType errorCode, T result, Exception? error) : IServiceResult
{
    public T Result { get; } = result;
    public ServiceResultType ErrorCode { get; } = errorCode;
    public Exception? Error { get; } = error;
}

public class ServiceResult(ServiceResultType code, Exception? error) : IServiceResult
{
    public ServiceResultType ErrorCode { get; } = code;
    public Exception? Error { get; } = error;
    public static ServiceResult Success() => new (ServiceResultType.Success, null);
    public static ServiceResult Fail(Exception error) => new(ServiceResultType.Error, error);
    public static ServiceResult FinishWithWarning(Exception error) => new(ServiceResultType.FinishWithWarning, error);
    public static ServiceResult<T> Success<T>(T result) => new (ServiceResultType.Success, result, null);
    public static ServiceResult<T> Fail<T>(T defaultResult, Exception error) => new(ServiceResultType.Error, defaultResult, error);
    public static ServiceResult<T> FinishWithWarning<T>(T result, Exception error) => new(ServiceResultType.FinishWithWarning, result, error);
}

public enum ServiceResultType
{
    Success,
    Error,
    FinishWithWarning,
}
