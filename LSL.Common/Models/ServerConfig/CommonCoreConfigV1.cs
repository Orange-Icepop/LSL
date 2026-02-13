using FluentResults;
using Mutty;

namespace LSL.Common.Models.ServerConfig;

[MutableGeneration]
public record CommonCoreConfigV1 : ICoreConfig<CommonCoreConfigV1>
{
    public int ConfigVersion => 1;
    public string JarName { get; init; } = string.Empty;
    
    public Result Validate(string parent)
    {
        try
        {
            if (!Directory.Exists(parent)) return Result.Fail("Invalid server directory.");
            var jarPath = Path.Combine(parent, JarName);
            if (!File.Exists(jarPath)) return Result.Fail($"Jar file at {jarPath} not found.");
            if (!jarPath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) return Result.Fail($"Invalid jar file {jarPath}.");
            return Result.Ok();
        }
        catch (Exception)
        {
            return Result.Fail("Invalid character in jar path.");
        }
    }

    public Task<Result<CommonCoreConfigV1>> ValidateAndFix(string parent)
    {
        return Task.FromResult(Validate(parent).Bind(() => Result.Ok(this)));
    }
}

public static class MutableCommonCoreConfigExtensions
{
    public static Result Validate(this MutableCommonCoreConfigV1 config, string parent)
    {
        try
        {
            if (!Directory.Exists(parent)) return Result.Fail("Invalid server directory.");
            var jarPath = Path.Combine(parent, config.JarName);
            if (!File.Exists(jarPath)) return Result.Fail($"Jar file at {jarPath} not found.");
            if (!jarPath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) return Result.Fail($"Invalid jar file {jarPath}.");
            return Result.Ok();
        }
        catch (Exception)
        {
            return Result.Fail("Invalid character in jar path.");
        }
    }

    public static Task<Result<CommonCoreConfigV1>> ValidateAndFix(this MutableCommonCoreConfigV1 config, string parent)
    {
        return Task.FromResult(config.Validate(parent).Bind(() => Result.Ok(config.FinishDraft())));
    }
}