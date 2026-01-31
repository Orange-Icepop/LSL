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

    public static async Task<Result<TResult>> Bind<T, TResult>(this Task<Result<T>> resultTask,
        Func<T, Result<TResult>> binder)
    {
        return (await resultTask).Bind(binder);
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

    public static async Task<Result<TResult>> BindAsync<T, TResult>(
        this Task<Result<T>> taskResult,
        Func<T, Task<Result<TResult>>> binder)
    {
        return await (await taskResult).BindAsync(binder);
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

    #region GetValueOrThrow

    public static T GetValueOrThrow<T>(this Result<T> result, bool ignoreWarning = true)
    {
        if (result.IsFailed || result.IsWarning && !ignoreWarning) throw result.Error;
        return result.Value;
    }

    public static async Task<T> GetValueOrThrow<T>(this Task<Result<T>> result, bool ignoreWarning = true)
    {
        return (await result).GetValueOrThrow(ignoreWarning);
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
    public static async Task<Result<T>> Match<T>(this Task<Result<T>> resultTask, Action<T>? onSuccess, Action<T, Exception>? onWarning,
        Action<Exception>? onFailure)
    {
        return (await resultTask).Match(onSuccess, onWarning, onFailure);
    }

    public static async Task<Result<T>> MatchAsync<T>(this Result<T> result, Func<T, Task>? onSuccess,
        Func<T, Exception, Task>? onWarning, Func<Exception, Task>? onFailure)
    {
        if (result.IsSuccess && onSuccess is not null) await onSuccess(result.Value);
        else if (result.IsWarning && onWarning is not null) await onWarning(result.Value, result.Error);
        else if (result.IsFailed && onFailure is not null) await onFailure(result.Error);
        return result;
    }
    public static async Task<Result<T>> MatchAsync<T>(this Task<Result<T>> resultTask, Func<T, Task>? onSuccess,
        Func<T, Exception, Task>? onWarning, Func<Exception, Task>? onFailure)
    {
        return await (await resultTask).MatchAsync(onSuccess, onWarning, onFailure);
    }

    #endregion

    public static Task<Result<Unit>> AsGeneric(this Task<Result> taskResult) =>
        taskResult.ContinueWith(Result<Unit> (t) => t.Result);
    public static Result AsSimple(this Result<Unit> result) => new(result);

    public static async Task<Result> AsSimple(this Task<Result<Unit>> taskResult) => new(await taskResult);
}