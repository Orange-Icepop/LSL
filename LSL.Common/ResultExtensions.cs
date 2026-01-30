namespace LSL.Common;

#region Bind

public static class ResultBinder
{
    public static ServiceResult<TResult> Bind<T, TResult>(
        this ServiceResult<T> result,
        Func<T, ServiceResult<TResult>> binder)
    {
        if (result.IsError) return ServiceResult.Fail<TResult>(result.Error!);

        if (result.IsWarning)
        {
            var nextResult = binder(result.Value);
            if (nextResult.IsSuccess) return ServiceResult.Warning(nextResult.Value, result.Error!);
            if (nextResult.IsWarning)
                return ServiceResult.Warning(nextResult.Value,
                    new AggregateException(result.Error!, nextResult.Error));
            return ServiceResult.Fail<TResult>(new AggregateException(result.Error!, nextResult.Error));
        }

        return binder(result.Value!);
    }

    public static ServiceResult Bind<T>(
        this ServiceResult<T> result,
        Func<T, ServiceResult> binder)
    {
        if (result.IsError) return ServiceResult.Fail(result.Error);

        if (result.IsWarning)
        {
            var nextResult = binder(result.Value);
            if (nextResult.IsSuccess) return ServiceResult.Warning(result.Error);
            if (nextResult.IsWarning)
                return ServiceResult.Warning(new AggregateException(result.Error, nextResult.Error));
            return ServiceResult.Fail(new AggregateException(result.Error!, nextResult.Error));
        }

        return binder(result.Value!);
    }

    public static async Task<ServiceResult<TResult>> BindAsync<T, TResult>(
        this ServiceResult<T> result,
        Func<T, Task<ServiceResult<TResult>>> binder)
    {
        if (result.IsError) return ServiceResult.Fail<TResult>(result.Error);

        if (result.IsWarning)
        {
            var nextResult = await binder(result.Value);
            if (nextResult.IsSuccess) return ServiceResult.Warning(nextResult.Value, result.Error!);
            if (nextResult.IsWarning)
                return ServiceResult.Warning(nextResult.Value,
                    new AggregateException(result.Error!, nextResult.Error));
            return ServiceResult.Fail<TResult>(new AggregateException(result.Error!, nextResult.Error));
        }

        return await binder(result.Value);
    }

    public static async Task<ServiceResult> BindAsync<T>(
        this ServiceResult<T> result,
        Func<T, Task<ServiceResult>> binder)
    {
        if (result.IsError) return ServiceResult.Fail(result.Error);

        if (result.IsWarning)
        {
            var nextResult = await binder(result.Value);
            if (nextResult.IsSuccess) return ServiceResult.Warning(result.Error!);
            if (nextResult.IsWarning)
                return ServiceResult.Warning(new AggregateException(result.Error!, nextResult.Error));
            return ServiceResult.Fail(new AggregateException(result.Error!, nextResult.Error));
        }

        return await binder(result.Value);
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

public static class ResultMapper
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

public static class ResultTapper
{
    public static ServiceResult<T> Tap<T>(this ServiceResult<T> result, Action<T> action, bool acceptWarning = false)
    {
        if (result.IsSuccess || result.IsWarning && acceptWarning)
        {
            action(result.Value);
        }

        return result;
    }

    public static async Task<ServiceResult<T>> Tap<T>(this Task<ServiceResult<T>> result, Action<T> action,
        bool acceptWarning = false)
    {
        return (await result.ConfigureAwait(false)).Tap(action, acceptWarning);
    }

    public static async Task<ServiceResult<T>> TapAsync<T>(this ServiceResult<T> result, Func<T, Task> action,
        bool acceptWarning = false)
    {
        if (result.IsSuccess || result.IsWarning && acceptWarning)
        {
            await action(result.Value);
        }

        return result;
    }

    public static async Task<ServiceResult<T>> TapAsync<T>(this Task<ServiceResult<T>> result, Func<T, Task> action,
        bool acceptWarning = false)
    {
        return await (await result.ConfigureAwait(false)).TapAsync(action, acceptWarning);
    }
}

#endregion

#region Unwrap

public static class ResultUnwrapper
{
    public static ServiceResult<T> Unwrap<T>(this ServiceResult<T> result, bool ignoreWarning = true)
    {
        if (result.IsError || result.IsWarning && !ignoreWarning) throw result.Error;
        return result;
    }

    public static async Task<ServiceResult<T>> Unwrap<T>(this Task<ServiceResult<T>> result, bool ignoreWarning = true)
    {
        return (await result).Unwrap(ignoreWarning);
    }
}

#endregion