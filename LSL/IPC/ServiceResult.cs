using System;

namespace LSL.IPC;

public interface IServiceResult
{
    ServiceResultType ErrorCode { get; }
    Exception? Error { get; }
}
public struct VoidResult()
{
    public static VoidResult Default => new VoidResult();
}
/*
 * ServiceError使用说明
 * ErrorCode为0时表示没有错误。
 * 1:执行完成，但是包含警告。
 * 2:出现错误。只代表最基础的错误返回。
 * -1:在没有执行完成的情况下中断了。
 */
public class ServiceResult<T> : IServiceResult
{
    public T Result { get; }
    public ServiceResultType ErrorCode { get; }
    public Exception? Error { get; }
    public ServiceResult(T result)
    {
        ErrorCode = ServiceResultType.Success;
        Result = result;
        Error = null;
    }
    public ServiceResult(ServiceResultType errorCode, T result, Exception error)
    {
        ErrorCode = errorCode;
        Result = result;
        Error = error;
    }
    public static ServiceResult<T> Fail(T result, Exception ex) => new(ServiceResultType.Error, result, ex);
}

public class ServiceResult(ServiceResultType code, Exception? error) : IServiceResult
{
    public ServiceResultType ErrorCode { get; } = code;
    public Exception? Error { get; } = error;
    public static ServiceResult Fail(Exception error) => new(ServiceResultType.Error, error);
    public static ServiceResult Success => new (ServiceResultType.Success, null);
}

public enum ServiceResultType
{
    Success,
    Error,
    FinishWithError,
}
