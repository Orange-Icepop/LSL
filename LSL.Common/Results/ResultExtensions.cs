namespace LSL.Common.Results;

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

    #endregion

    #region GetValueOrThrow

    public static T GetValueOrThrow<T>(this Result<T> result, bool ignoreWarning = true)
    {
        if (result.IsFailed || result.IsWarning && !ignoreWarning) throw result.Error;
        return result.Value;
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

    public static async Task<Result<T>> Match<T>(this Task<Result<T>> resultTask, Action<T>? onSuccess,
        Action<T, Exception>? onWarning,
        Action<Exception>? onFailure)
    {
        return (await resultTask).Match(onSuccess, onWarning, onFailure);
    }

    #endregion
}