namespace LSL.Common.Models;

#region Bind
public static class ServiceResultBinder
{


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

}
#endregion

#region Map
public static class ServiceResultMap
{
    public static ServiceResult<TResult> Map<T, TResult>(
        this ServiceResult<T> result,
        Func<T, TResult> mapper)
    {
        return result.Bind(x =>
        {
            try
            {
                return ServiceResult.Success(mapper(x));
            }
            catch (Exception e)
            {
                return ServiceResult.Fail<TResult>(e);
            }
        });
    }
    public static Task<ServiceResult<TResult>> MapAsync<T, TResult>(
        this ServiceResult<T> result,
        Func<T, Task<TResult>> mapper)
    {
        return result.BindAsync(async x =>
        {
            try
            {
                return ServiceResult.Success(await mapper(x));
            }
            catch (Exception e)
            {
                return ServiceResult.Fail<TResult>(e);
            }
        });
    }
}
#endregion

#region Tap
public static class ServiceResultTap
{
    public static ServiceResult<T> Tap<T>(this ServiceResult<T> result, Action<T> action, bool acceptWarning = false)
    {
        if (result.IsSuccess || result.IsWarning && acceptWarning)
        {
            action(result.Result);
        }

        return result;
    }

    public static async Task<ServiceResult<T>> Tap<T>(this Task<ServiceResult<T>> result, Action<T> action,
        bool acceptWarning = false)
    {
        return (await result.ConfigureAwait(false)).Tap(action, acceptWarning);
    }
    public static async Task<ServiceResult<T>> TapAsync<T>(this ServiceResult<T> result, Func<T, Task> action, bool acceptWarning = false)
    {
        if (result.IsSuccess || result.IsWarning && acceptWarning)
        {
            await action(result.Result);
        }

        return result;
    }
    public static async Task<ServiceResult<T>> TapAsync<T>(this Task<ServiceResult<T>> result, Func<T, Task> action, bool acceptWarning = false)
    {
        return await (await result.ConfigureAwait(false)).TapAsync(action, acceptWarning);
    }
}
#endregion