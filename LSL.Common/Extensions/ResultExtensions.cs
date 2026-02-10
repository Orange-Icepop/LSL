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
    public static async Task<Result> Tap(this Result result, Func<Result,Task> predicate)
    {
        await predicate(result);
        return result;
    }
    public static async Task<Result> Tap(this Task<Result> result, Func<Result,Task> predicate)
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
            if (throwWarnings && warnings.Count>0)
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

    public static Result Handle(this Result result, Action onSuccess,
        Action<IReadOnlyList<Exception>> onWarning, Action<IReadOnlyList<Exception>> onFailure)
    {
        if (result.IsFailed)
        {
            onFailure(result.GetErrors());
        }
        else
        {
            var warnings = result.GetWarnings();
            if (warnings.Count == 0) onSuccess();
            else onWarning(warnings);
        }
        return result;
    }
    public static async Task<Result> Handle(this Task<Result> result, Action onSuccess,
        Action<IReadOnlyList<Exception>> onWarning, Action<IReadOnlyList<Exception>> onFailure) =>
        (await result).Handle(onSuccess, onWarning, onFailure);

    public static Result<TResult> Handle<TResult>(this Result<TResult> result, Action<TResult> onSuccess,
        Action<TResult, IReadOnlyList<Exception>> onWarning, Action<IReadOnlyList<Exception>> onFailure)
    {
        if (result.IsFailed)
        {
            onFailure(result.GetErrors());
        }
        else
        {
            var warnings = result.GetWarnings();
            if (warnings.Count == 0) onSuccess(result.Value);
            else onWarning(result.Value, warnings);
        }
        return result;
    }
    public static async Task<Result<TResult>> Handle<TResult>(this Task<Result<TResult>> result,
        Action<TResult> onSuccess,
        Action<TResult, IReadOnlyList<Exception>> onWarning,
        Action<IReadOnlyList<Exception>> onFailure) => (await result).Handle(onSuccess, onWarning, onFailure);

    #endregion
    
    public static Result ToResult(this AggregateException aggregate)
    {
        return Result.Fail(aggregate.InnerExceptions.Select(e => new ExceptionalError(e)));
    }
    public static Result<TResult> ToResult<TResult>(this AggregateException aggregate)
    {
        return Result.Fail<TResult>(aggregate.InnerExceptions.Select(e => new ExceptionalError(e)));
    }
}