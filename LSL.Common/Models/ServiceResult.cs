namespace LSL.Common.Models;

public interface IServiceResult
{
    ServiceResultType ErrorCode { get; }
    Exception? Error { get; }
}

public class ServiceResult<T> : IServiceResult
{
    public T? Result { 
        get 
        {
            if (!HasResult) 
            {
                if (Error is not null) throw Error; 
                else throw new InvalidOperationException("Cannot access Result on failed operation");
            }
            return _result;
        }
    }
    
    public ServiceResultType ErrorCode { get; }
    public Exception? Error { get; }
    public bool IsFullSuccess => ErrorCode == ServiceResultType.Success;
    public bool HasResult => ErrorCode != ServiceResultType.Error;
    public bool IsFullError => ErrorCode == ServiceResultType.Error;
    public bool HasError => ErrorCode != ServiceResultType.Success;

    private readonly T? _result;

    // 内部构造器防止不规范创建
    internal ServiceResult(ServiceResultType errorCode, T? result, Exception? error)
    {
        ErrorCode = errorCode;
        _result = result;
        Error = error;
    }
}

public class ServiceResult(ServiceResultType code, Exception? error) : IServiceResult
{
    public ServiceResultType ErrorCode { get; } = code;
    public Exception? Error { get; } = error;
    public bool IsFullSuccess => ErrorCode == ServiceResultType.Success;
    public bool IsFullError => ErrorCode == ServiceResultType.Error;
    public bool HasError => ErrorCode != ServiceResultType.Success;
    
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
