using System.ComponentModel;
using FluentResults;
using Mutty;

namespace LSL.Common.Models.ServerConfig;

[MutableGeneration]
public record CommonCoreConfigV1 : ICoreConfig<CommonCoreConfigV1>
{
    public int ConfigVersion => 1;
    public string JarName { get; init; } = string.Empty;
    
    //TODO:合并逻辑
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

public partial class MutableCommonCoreConfigV1 : INotifyPropertyChanged
{
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
        return Task.FromResult(Validate(parent).Bind(() => Result.Ok(this.FinishDraft())));
    }
}