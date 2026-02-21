using FluentResults;

namespace LSL.Common.Models.ServerConfig;

public interface ICoreConfig<T> where T : ICoreConfig<T>
{
    Result Validate(string parent);
    Task<Result<T>> ValidateAndFix(string parent);
}