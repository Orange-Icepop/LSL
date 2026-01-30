namespace LSL.Common;

public static class ResultExtensions
{
    #region Bind

    public static Result<TResult> Bind<T, TResult>(
        this Result<T> result,
        Func<T, Result<TResult>> binder)
    {
        if (result.IsFailed) return Result.Fail<TResult>(result.Error!);

        if (result.IsWarning)
        {
            var nextResult = binder(result.Value);
            if (nextResult.IsSuccess) return Result.Warning(nextResult.Value, result.Error!);
            if (nextResult.IsWarning)
                return Result.Warning(nextResult.Value,
                    new AggregateException(result.Error!, nextResult.Error));
            return Result.Fail<TResult>(new AggregateException(result.Error!, nextResult.Error));
        }

        return binder(result.Value!);
    }

    public static Result Bind<T>(
        this Result<T> result,
        Func<T, Result> binder)
    {
        if (result.IsFailed) return Result.Fail(result.Error);

        if (result.IsWarning)
        {
            var nextResult = binder(result.Value);
            if (nextResult.IsSuccess) return Result.Warning(result.Error);
            if (nextResult.IsWarning)
                return Result.Warning(new AggregateException(result.Error, nextResult.Error));
            return Result.Fail(new AggregateException(result.Error!, nextResult.Error));
        }

        return binder(result.Value!);
    }

    public static async Task<Result<TResult>> BindAsync<T, TResult>(
        this Result<T> result,
        Func<T, Task<Result<TResult>>> binder)
    {
        if (result.IsFailed) return Result.Fail<TResult>(result.Error);

        if (result.IsWarning)
        {
            var nextResult = await binder(result.Value);
            if (nextResult.IsSuccess) return Result.Warning(nextResult.Value, result.Error!);
            if (nextResult.IsWarning)
                return Result.Warning(nextResult.Value,
                    new AggregateException(result.Error!, nextResult.Error));
            return Result.Fail<TResult>(new AggregateException(result.Error!, nextResult.Error));
        }

        return await binder(result.Value);
    }

    public static async Task<Result> BindAsync<T>(
        this Result<T> result,
        Func<T, Task<Result>> binder)
    {
        if (result.IsFailed) return Result.Fail(result.Error);

        if (result.IsWarning)
        {
            var nextResult = await binder(result.Value);
            if (nextResult.IsSuccess) return Result.Warning(result.Error!);
            if (nextResult.IsWarning)
                return Result.Warning(new AggregateException(result.Error!, nextResult.Error));
            return Result.Fail(new AggregateException(result.Error!, nextResult.Error));
        }

        return await binder(result.Value);
    }

    public static async Task<Result<TResult>> BindAsync<T, TResult>(
        this Task<Result<T>> taskResult,
        Func<T, Task<Result<TResult>>> binder)
    {
        var result = await taskResult;
        return await result.BindAsync(binder);
    }

    public static async Task<Result> BindAsync<T>(
        this Task<Result<T>> taskResult,
        Func<T, Task<Result>> binder)
    {
        var result = await taskResult;
        return await result.BindAsync(binder);
    }

    #endregion

    #region Map

    public static Result<TResult> Map<T, TResult>(
        this Result<T> result,
        Func<T, TResult> mapper)
    {
        return result.Bind(x =>
        {
            try
            {
                return Result.Success(mapper(x));
            }
            catch (Exception e)
            {
                return Result.Fail<TResult>(e);
            }
        });
    }

    public static Task<Result<TResult>> MapAsync<T, TResult>(
        this Result<T> result,
        Func<T, Task<TResult>> mapper)
    {
        return result.BindAsync(async x =>
        {
            try
            {
                return Result.Success(await mapper(x));
            }
            catch (Exception e)
            {
                return Result.Fail<TResult>(e);
            }
        });
    }

    #endregion

    #region Tap

    public static Result<T> Tap<T>(this Result<T> result, Action<T> action, bool acceptWarning = false)
    {
        if (result.IsSuccess || result.IsWarning && acceptWarning)
        {
            action(result.Value);
        }

        return result;
    }

    public static async Task<Result<T>> Tap<T>(this Task<Result<T>> result, Action<T> action,
        bool acceptWarning = false)
    {
        return (await result.ConfigureAwait(false)).Tap(action, acceptWarning);
    }

    public static async Task<Result<T>> TapAsync<T>(this Result<T> result, Func<T, Task> action,
        bool acceptWarning = false)
    {
        if (result.IsSuccess || result.IsWarning && acceptWarning)
        {
            await action(result.Value);
        }

        return result;
    }

    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> result, Func<T, Task> action,
        bool acceptWarning = false)
    {
        return await (await result.ConfigureAwait(false)).TapAsync(action, acceptWarning);
    }

    #endregion

    #region Unwrap

    public static Result<T> Unwrap<T>(this Result<T> result, bool ignoreWarning = true)
    {
        if (result.IsFailed || result.IsWarning && !ignoreWarning) throw result.Error;
        return result;
    }

    public static async Task<Result<T>> Unwrap<T>(this Task<Result<T>> result, bool ignoreWarning = true)
    {
        return (await result).Unwrap(ignoreWarning);
    }

    #endregion

    #region Match

    public static Result<T> Match<T>(this Result<T> result, Action<T>? onSuccess, Action<T, Exception>? onWarning,
        Action<Exception>? onFailure)
    {
        if (result.IsSuccess) onSuccess?.Invoke(result.Value);
        else if (result.IsWarning) onWarning?.Invoke(result.Value, result.Error);
        else onFailure?.Invoke(result.Error);
        return result;
    }

    public static async Task<Result<T>> HandleAsync<T>(this Result<T> result, Func<T, Task>? onSuccess,
        Func<T, Exception, Task>? onWarning, Func<Exception, Task>? onFailure)
    {
        if (result.IsSuccess && onSuccess is not null) await onSuccess(result.Value);
        else if (result.IsWarning && onWarning is not null) await onWarning(result.Value, result.Error);
        else if (result.IsFailed && onFailure is not null) await onFailure(result.Error);
        return result;
    }

    #endregion
}