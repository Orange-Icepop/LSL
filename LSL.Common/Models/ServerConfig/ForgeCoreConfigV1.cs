using FluentResults;
using LSL.Common.Utilities.Minecraft;
using Mutty;

namespace LSL.Common.Models.ServerConfig;

[MutableGeneration]
public record ForgeCoreConfigV1 : ICoreConfig<ForgeCoreConfigV1>
{
    public ForgeCoreConfigV1()
    {
        UnixLibraryArgsPath = string.Empty;
        WinLibraryArgsPath = string.Empty;
    }

    public ForgeCoreConfigV1(string unixLibraryArgs, string winLibraryArgs)
    {
        UnixLibraryArgsPath = unixLibraryArgs;
        WinLibraryArgsPath = winLibraryArgs;
    }

    public int ConfigVersion => 1;
    public string UnixLibraryArgsPath { get; init; }
    public string WinLibraryArgsPath { get; init; }

    public Result Validate(string parent)
    {
        try
        {
            if (!File.Exists(Path.Combine(parent, UnixLibraryArgsPath))) return Result.Fail("Unix library argument file does not exist.");
            if (!File.Exists(Path.Combine(parent, WinLibraryArgsPath))) return Result.Fail("Windows library argument file does not exist.");
            return Result.Ok();
        }
        catch (Exception)
        {
            return Result.Fail("Invalid character in argument path.");
        }
    }

    public async Task<Result<ForgeCoreConfigV1>> ValidateAndFix(string parent)
    {
        try
        {
            if (File.Exists(Path.Combine(parent, WinLibraryArgsPath)) &&
                File.Exists(Path.Combine(parent, UnixLibraryArgsPath))) return Result.Ok(this);
            var detectResult = await ForgeConfigHelper.GetForgeConfig(parent);
            if (detectResult.IsFailed)
                return Result.Fail<ForgeCoreConfigV1>("Cannot get the correct core info of the forge server");
            return detectResult;
        }
        catch (Exception)
        {
            return Result.Fail<ForgeCoreConfigV1>("Invalid character in argument path.");
        }
    }
}