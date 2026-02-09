using System.Runtime.CompilerServices;

namespace LSL.Common.Results;

public static class AsyncResultExtensions
{
    #region Bind

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

    public static async Task<T> GetValueOrThrow<T>(this Task<Result<T>> result, bool ignoreWarning = true)
    {
        return (await result).GetValueOrThrow(ignoreWarning);
    }

    #endregion

    #region Match

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<Result<Unit>> AsGeneric(this Task<Result> taskResult) => await taskResult.ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<Result> AsSimple(this Task<Result<Unit>> taskResult) => await taskResult.ConfigureAwait(false);
}