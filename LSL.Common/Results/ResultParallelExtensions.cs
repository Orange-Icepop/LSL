using System.Collections.Concurrent;

namespace LSL.Common.Results;

public static class ResultParallelExtensions
{
    public static async Task<(AggregateException? Warnings, AggregateException? Errors)> WhenAll(this IEnumerable<Task<IResult>> tasks)
    {
        ConcurrentBag<Exception> warnings = [];
        ConcurrentBag<Exception> errors = [];
        var proceedTasks = tasks.Select(async task =>
        {
            var result = await task;
            if (result.Error is not null)
            {
                if (result.Kind is ResultType.Warning) warnings.Add(result.Error);
                else errors.Add(result.Error);
            }
        });
        await Task.WhenAll(proceedTasks);
        return (new AggregateException(warnings).Flatten(), new AggregateException(errors).Flatten());
    }

    public static async Task<IResult> AsIResult<T>(this Task<Result<T>> task)
    {
        return await task;
    }
}