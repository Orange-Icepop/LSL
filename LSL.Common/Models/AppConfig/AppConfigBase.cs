using FluentResults;
using Tomlyn;

namespace LSL.Common.Models.AppConfig;

public abstract record AppConfigBase<TConfig> : IConfig<TConfig> where TConfig : AppConfigBase<TConfig>, new()
{
    public virtual string Serialize()
    {
        return Toml.FromModel(this);
    }

    public abstract Result Validate();
    public abstract Result<TConfig> ValidateAndFix();

    public static Result<TConfig> Deserialize(string content)
    {
        if (!Toml.TryToModel<TConfig>(content, out var result, out var error))
            return Result.Warning(new TConfig(), $"The {typeof(TConfig).Name} is not parsable:\n{error}");
        Result<Unit> validationResult = result.Validate();
        return validationResult.IsSuccess ? Result.Ok(result) : Result.Warning(result, validationResult.Error);
    }
}