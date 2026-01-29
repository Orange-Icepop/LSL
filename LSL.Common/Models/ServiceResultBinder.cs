namespace LSL.Common.Models;

public static class ServiceResultBinder
{
    #region Synchronous

    public static ServiceResult<TResult> Bind<T, TResult>(
        this ServiceResult<T> result,
        Func<T, ServiceResult<TResult>> binder)
    {
        if (result.IsError) return ServiceResult.Fail<TResult>(result.Error!);

        if (result.IsWarning)
        {
            var nextResult = binder(result.Result);
            if (nextResult.IsSuccess) return ServiceResult.Warning(nextResult.Result, result.Error!);
            if (nextResult.IsWarning)
                return ServiceResult.Warning(nextResult.Result,
                    new AggregateException(result.Error!, nextResult.Error));
            return ServiceResult.Fail<TResult>(new AggregateException(result.Error!, nextResult.Error));
        }

        return binder(result.Result!);
    }

    public static ServiceResult Bind<T>(
        this ServiceResult<T> result,
        Func<T, ServiceResult> binder)
    {
        if (result.IsError) return ServiceResult.Fail(result.Error);

        if (result.IsWarning)
        {
            var nextResult = binder(result.Result);
            if (nextResult.IsSuccess) return ServiceResult.Warning(result.Error);
            if (nextResult.IsWarning)
                return ServiceResult.Warning(new AggregateException(result.Error, nextResult.Error));
            return ServiceResult.Fail(new AggregateException(result.Error!, nextResult.Error));
        }

        return binder(result.Result!);
    }

    #endregion
    
    #region Asynchronous

    public static async Task<ServiceResult<TResult>> BindAsync<T, TResult>(
        this ServiceResult<T> result,
        Func<T, Task<ServiceResult<TResult>>> binder)
    {
        if (result.IsError) return ServiceResult.Fail<TResult>(result.Error);

        if (result.IsWarning)
        {
            var nextResult = await binder(result.Result);
            if (nextResult.IsSuccess) return ServiceResult.Warning(nextResult.Result, result.Error!);
            if (nextResult.IsWarning)
                return ServiceResult.Warning(nextResult.Result,
                    new AggregateException(result.Error!, nextResult.Error));
            return ServiceResult.Fail<TResult>(new AggregateException(result.Error!, nextResult.Error));
        }

        return await binder(result.Result);
    }

    public static async Task<ServiceResult> BindAsync<T>(
        this ServiceResult<T> result,
        Func<T, Task<ServiceResult>> binder)
    {
        if (result.IsError) return ServiceResult.Fail(result.Error);

        if (result.IsWarning)
        {
            var nextResult = await binder(result.Result);
            if (nextResult.IsSuccess) return ServiceResult.Warning(result.Error!);
            if (nextResult.IsWarning)
                return ServiceResult.Warning(new AggregateException(result.Error!, nextResult.Error));
            return ServiceResult.Fail(new AggregateException(result.Error!, nextResult.Error));
        }

        return await binder(result.Result);
    }

    public static async Task<ServiceResult<TResult>> BindAsync<T, TResult>(
        this Task<ServiceResult<T>> taskResult,
        Func<T, Task<ServiceResult<TResult>>> binder)
    {
        var result = await taskResult;
        return await result.BindAsync(binder);
    }

    public static async Task<ServiceResult> BindAsync<T>(
        this Task<ServiceResult<T>> taskResult,
        Func<T, Task<ServiceResult>> binder)
    {
        var result = await taskResult;
        return await result.BindAsync(binder);
    }

    #endregion
}