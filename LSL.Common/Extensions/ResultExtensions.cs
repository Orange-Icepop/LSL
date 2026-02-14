using FluentResults;
using LSL.Common.Models;

namespace LSL.Common.Extensions;

public static class ResultExtensions
{
    #region Tap

    public static Result Tap(this Result result, Action<Result> predicate)
    {
        predicate(result);
        return result;
    }

    public static async Task<Result> Tap(this Task<Result> result, Action<Result> predicate)
    {
        var res = await result;
        predicate(res);
        return res;
    }

    public static async Task<Result> Tap(this Result result, Func<Result, Task> predicate)
    {
        await predicate(result);
        return result;
    }

    public static async Task<Result> Tap(this Task<Result> result, Func<Result, Task> predicate)
    {
        var res = await result;
        await predicate(res);
        return res;
    }
    public static Result<T> Tap<T>(this Result<T> result, Action<Result<T>> predicate)
    {
        predicate(result);
        return result;
    }

    public static async Task<Result<T>> Tap<T>(this Task<Result<T>> result, Action<Result<T>> predicate)
    {
        var res = await result;
        predicate(res);
        return res;
    }

    public static async Task<Result<T>> Tap<T>(this Result<T> result, Func<Result<T>, Task> predicate)
    {
        await predicate(result);
        return result;
    }

    public static async Task<Result<T>> Tap<T>(this Task<Result<T>> result, Func<Result<T>, Task> predicate)
    {
        var res = await result;
        await predicate(res);
        return res;
    }

    #endregion

    public static IReadOnlyList<Exception> GetWarnings(this IResultBase result)
    {
        if (result.IsFailed) return [];
        var warnings = result.Reasons.OfType<IWarning>().ToList();
        if (warnings.Count == 0) return [];

        return warnings.Select(w => w switch
        {
            ExceptionalWarningReason ewr => ewr.Exception,
            _ => new Exception(w.Message)
        }).ToList();
    }

    public static IReadOnlyList<Exception> GetErrors(this IResultBase result)
    {
        if (result.IsSuccess || result.Errors.Count == 0) return [];
        return result.Errors.Select(e => e switch
        {
            ExceptionalError er => er.Exception,
            _ => new Exception(e.Message)
        }).ToList();
    }

    #region PackAndThrowIfFailed

    public static Result<TResult> PackAndThrowIfFailed<TResult>(
        this Result<TResult> result,
        bool throwWarnings = false,
        bool packWarningsIfFail = false)
    {
        ThrowIfNeeded(result, throwWarnings, packWarningsIfFail);
        return result;
    }

    public static Result PackAndThrowIfFailed(
        this Result result,
        bool throwWarnings = false,
        bool packWarningsIfFail = false)
    {
        ThrowIfNeeded(result, throwWarnings, packWarningsIfFail);
        return result;
    }

    private static void ThrowIfNeeded(
        IResultBase result,
        bool throwWarnings,
        bool packWarningsIfFail)
    {
        var warnings = result.GetWarnings();

        if (result.IsSuccess)
        {
            if (throwWarnings && warnings.Count > 0)
            {
                throw new AggregateException(warnings);
            }

            return;
        }

        var exceptions = packWarningsIfFail
            ? warnings.Concat(result.GetErrors()).ToList()
            : result.GetErrors().ToList();

        if (exceptions.Count > 0)
        {
            throw new AggregateException(exceptions);
        }
    }

    public static async Task<Result<TResult>> PackAndThrowIfFailed<TResult>(this Task<Result<TResult>> task,
        bool throwWarnings = false, bool packWarningsIfFail = false) =>
        (await task).PackAndThrowIfFailed(throwWarnings, packWarningsIfFail);

    public static async Task<Result> PackAndThrowIfFailed(this Task<Result> task,
        bool throwWarnings = false, bool packWarningsIfFail = false) =>
        (await task).PackAndThrowIfFailed(throwWarnings, packWarningsIfFail);

    #endregion

    #region Handle

    public static Result Handle(this Result result, Action? onSuccess = null,
        Action<IReadOnlyList<Exception>>? onWarning = null, Action<IReadOnlyList<Exception>>? onFailure = null)
    {
        if (result.IsFailed)
        {
            onFailure?.Invoke(result.GetErrors());
        }
        else
        {
            var warnings = result.GetWarnings();
            if (warnings.Count == 0) onSuccess?.Invoke();
            else onWarning?.Invoke(warnings);
        }

        return result;
    }

    public static async Task<Result> Handle(this Task<Result> result, Action? onSuccess = null,
        Action<IReadOnlyList<Exception>>? onWarning = null, Action<IReadOnlyList<Exception>>? onFailure = null) =>
        (await result).Handle(onSuccess, onWarning, onFailure);

    public static Result<TResult> Handle<TResult>(this Result<TResult> result, Action<TResult>? onSuccess = null,
        Action<TResult, IReadOnlyList<Exception>>? onWarning = null, Action<IReadOnlyList<Exception>>? onFailure = null)
    {
        if (result.IsFailed)
        {
            onFailure?.Invoke(result.GetErrors());
        }
        else
        {
            var warnings = result.GetWarnings();
            if (warnings.Count == 0) onSuccess?.Invoke(result.Value);
            else onWarning?.Invoke(result.Value, warnings);
        }

        return result;
    }

    public static async Task<Result<TResult>> Handle<TResult>(this Task<Result<TResult>> result,
        Action<TResult>? onSuccess = null,
        Action<TResult, IReadOnlyList<Exception>>? onWarning = null,
        Action<IReadOnlyList<Exception>>? onFailure = null) => (await result).Handle(onSuccess, onWarning, onFailure);

    public static async Task<Result> Handle(this Result result, Func<Task>? onSuccess = null,
        Func<IReadOnlyList<Exception>, Task>? onWarning = null, Func<IReadOnlyList<Exception>, Task>? onFailure = null)
    {
        if (result.IsFailed)
        {
            if (onFailure is not null) await onFailure(result.GetErrors());
        }
        else
        {
            var warnings = result.GetWarnings();
            if (warnings.Count == 0)
            {
                if (onSuccess is not null) await onSuccess();
            }
            else
            {
                if (onWarning is not null) await onWarning(warnings);
            }
        }

        return result;
    }

    public static async Task<Result> Handle(this Task<Result> result, Func<Task>? onSuccess = null,
        Func<IReadOnlyList<Exception>, Task>? onWarning = null, Func<IReadOnlyList<Exception>, Task>? onFailure = null)
    {
        return await (await result).Handle(onSuccess, onWarning, onFailure);
    }

    public static async Task<Result<T>> Handle<T>(this Result<T> result, Func<T, Task>? onSuccess = null,
        Func<T, IReadOnlyList<Exception>, Task>? onWarning = null,
        Func<IReadOnlyList<Exception>, Task>? onFailure = null)
    {
        if (result.IsFailed)
        {
            if (onFailure is not null) await onFailure(result.GetErrors());
        }
        else
        {
            var warnings = result.GetWarnings();
            if (warnings.Count == 0)
            {
                if (onSuccess is not null) await onSuccess(result.Value);
            }
            else
            {
                if (onWarning is not null) await onWarning(result.Value, warnings);
            }
        }

        return result;
    }

    public static async Task<Result<T>> Handle<T>(this Task<Result<T>> result, Func<T, Task>? onSuccess = null,
        Func<T, IReadOnlyList<Exception>, Task>? onWarning = null,
        Func<IReadOnlyList<Exception>, Task>? onFailure = null)
    {
        return await (await result).Handle(onSuccess, onWarning, onFailure);
    }

    #endregion

    public static Result ToResult(this AggregateException aggregate)
    {
        return Result.Fail(aggregate.InnerExceptions.Select(e => new ExceptionalError(e)));
    }

    public static Result<TResult> ToResult<TResult>(this AggregateException aggregate)
    {
        return Result.Fail<TResult>(aggregate.InnerExceptions.Select(e => new ExceptionalError(e)));
    }

    public static async Task<ResultBase> Basify(this Task<Result> task) => await task.ConfigureAwait(false);
    public static async Task<ResultBase> Basify<T>(this Task<Result<T>> task) => await task.ConfigureAwait(false);
}