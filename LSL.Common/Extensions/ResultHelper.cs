using System.Collections.Concurrent;
using FluentResults;

namespace LSL.Common.Extensions;

public static class ResultHelper
{
    public static async Task<Result> ForEachAsync(params Task<ResultBase>[] tasks)
    {
        ConcurrentBag<IReason> reasons =[];
        ConcurrentBag<ISuccess> successes=[];
        ConcurrentBag<IError> errors= [];
        await Parallel.ForEachAsync(tasks, async (task, token) =>
        {
            var result = await task;
            foreach (var i in result.Reasons)
            {
                reasons.Add(i);
            }

            foreach (var i in result.Successes)
            {
                successes.Add(i);
            }

            foreach (var i in result.Errors)
            {
                errors.Add(i);
            }
        });
        return Result.Ok().WithReasons(reasons).WithSuccesses(successes).WithErrors(errors);
    }
}